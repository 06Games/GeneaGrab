import { createSignal, createEffect, onCleanup, onMount, createResource, Show } from "solid-js";
import { Kbd } from "../ui/primitives";
import { TopBar } from "../ui/TopBar";
import { MainViewer } from "../components/registry-viewer/MainViewer";
import { ThumbnailBar } from "../components/registry-viewer/ThumbnailBar";
import { InfoNotesPanel } from "../components/registry-viewer/InfoNotesPanel";
import { IndexPanel } from "../components/registry-viewer/index-panel/IndexPanel";
import { Icon } from "@iconify-icon/solid";
import { useDetachedWindow } from "../hooks/DetachedWindow";
import { useI18n } from "../ui/i18n";
import { RegistryActionsProvider } from "../contexts/RegistryActionsContext";
import { useBackend } from "../contexts/BackendContext";
import { EventDetail } from "../types";
import { UserImageMeta } from "../types/image";
import { RegistryMeta, UserRegistryMeta } from "../types/registry";
import { useCurrentTab, useTabs } from "../contexts/TabsContext";

type SyncMessage =
  | { type: "READY" }
  | { type: "SYNC_STATE"; selectedEventId: number | null }
  | { type: "DETACHED_CLOSED" }
  | { type: "TOGGLE_DETACHED" }
  | { type: "SELECT_EVENT"; id: number | null };

const MIN_INDEX_HEIGHT = 200;
const MAX_INDEX_HEIGHT = 700;
const DEFAULT_INDEX_HEIGHT = 340;

const MIN_THUMB_HEIGHT = 60;
const MAX_THUMB_HEIGHT = 200;
const DEFAULT_THUMB_HEIGHT = 125;

interface ViewerPageProps {
  registryId: number;
  initialImageId?: number;
}
export const ViewerPage = (props: ViewerPageProps) => {
  const { t } = useI18n();
  const { openTab } = useTabs();
  const { updateThisTab } = useCurrentTab();
  const api = useBackend();

  const registryId = props.registryId;
  const urlParams = new URLSearchParams(typeof window !== "undefined" ? window.location.search : "");
  const isDetachedMode = urlParams.get("mode") === "index";

  const [tabTile, setTableTitle] = createSignal("");
  const [currentImage, setCurrentImage] = createSignal(props.initialImageId || 1);
  createEffect(() => {
    if (props.initialImageId !== undefined) {
      setCurrentImage(props.initialImageId);
    }
  });
  const [indexVisible, setIndexVisible] = createSignal(false);
  const [indexHeight, setIndexHeight] = createSignal(DEFAULT_INDEX_HEIGHT);
  const [thumbnailHeight, setThumbnailHeight] = createSignal(DEFAULT_THUMB_HEIGHT);
  const [selectedEventId, setSelectedEventId] = createSignal<number | null>(null);

  const [registryMeta, { mutate: mutateRegistryMeta }] = createResource(() => registryId, api.getRegistryMeta);
  const [eventRows] = createResource(() => registryId, api.getEventRows);
  const [imageMeta, { mutate: mutateImageMeta }] = createResource(
    () => (isDetachedMode ? false : { regId: registryId, imgId: currentImage() }),
    ({ regId, imgId }) => api.getImageMeta(regId, imgId),
  );

  // Sync URL with current image
  createEffect(() => {
    const regMeta = registryMeta();
    if (!regMeta) return;
    setTableTitle(
      `${regMeta.places?.map((place) => place[place.length - 1]).join(", ") || t("infoPanel.unknown")} · ${
        regMeta.source_types?.size > 0
          ? Array.from(regMeta.source_types)
              .map((source_type) => source_type.label)
              .join(", ")
          : t("infoPanel.unknown")
      } (${regMeta.date_from ?? "?"}-${regMeta.date_to ?? "?"})`,
    );

    if (!isDetachedMode) {
      updateThisTab({
        title: tabTile(),
        imageId: currentImage(),
      });
    }
  });

  // Cache for event details to avoid refetching on every selection
  const [detailCache, setDetailCache] = createSignal<Record<number, EventDetail>>({});
  const fetchingIds = new Set<number>();
  const getEventDetail = (id: number): EventDetail | null => {
    const cached = detailCache()[id];
    if (cached) return cached;

    if (!fetchingIds.has(id)) {
      fetchingIds.add(id);
      api
        .getEventDetail(id)
        .then((detail) => {
          if (detail) setDetailCache((prev) => ({ ...prev, [id]: detail }));
        })
        .catch((err) => {
          console.error("Failed to fetch event detail:", err);
          fetchingIds.delete(id);
        });
    }
    return null;
  };

  // Sync with detached index window if open
  const { isDetached, setIsDetached, detach, closeSelf, sendMessage } = useDetachedWindow<SyncMessage>({
    id: `index-${registryId}`,
    title: `Index - ${registryId}`,
    queryParams: { mode: "index" },
    width: 1400,
    height: 600,
    onMessage: (msg) => {
      if (isDetachedMode) {
        if (msg.type === "SYNC_STATE") setSelectedEventId(msg.selectedEventId);
      } else {
        if (msg.type === "READY") {
          sendMessage({ type: "SYNC_STATE", selectedEventId: selectedEventId() });
        }
        if (msg.type === "DETACHED_CLOSED" || msg.type === "TOGGLE_DETACHED") {
          setIsDetached(false);
        }
        if (msg.type === "SELECT_EVENT") {
          setSelectedEventId(msg.id);
        }
      }
    },
  });

  onMount(() => {
    if (isDetachedMode) {
      sendMessage({ type: "READY" });
      const handleUnload = () => sendMessage({ type: "DETACHED_CLOSED" });
      window.addEventListener("beforeunload", handleUnload);
      onCleanup(() => window.removeEventListener("beforeunload", handleUnload));
    } else {
      window.addEventListener("keydown", onKeyDown);
      onCleanup(() => window.removeEventListener("keydown", onKeyDown));
    }
  });

  createEffect(() => {
    if (!isDetachedMode) {
      sendMessage({ type: "SYNC_STATE", selectedEventId: selectedEventId() });
    }
  });

  // Shortcuts
  const onKeyDown = (e: KeyboardEvent) => {
    const target = e.target as HTMLElement;
    const inInput = target.tagName === "INPUT" || target.tagName === "TEXTAREA";
    if (!inInput) {
      const total = registryMeta()?.total_images || 1;
      if (e.key === "ArrowLeft") setCurrentImage((p) => Math.max(1, p - 1));
      if (e.key === "ArrowRight") setCurrentImage((p) => Math.min(total, p + 1));
    }
    if (e.key === "i" && e.ctrlKey) {
      e.preventDefault();
      setIndexVisible((v) => !v);
    }
  };

  const handleCloseDetachedWindow = async () => {
    sendMessage({ type: "TOGGLE_DETACHED" });
    await closeSelf();
  };

  // Resize handlers
  const createResizeHandler = (startVal: () => number, setter: (val: number) => void, min: number, max: number) => {
    return (e: PointerEvent) => {
      e.preventDefault();
      const startY = e.clientY;
      const startH = startVal();
      const onMove = (ev: PointerEvent) => {
        setter(Math.max(min, Math.min(max, startH + (startY - ev.clientY))));
      };
      const onUp = () => {
        window.removeEventListener("pointermove", onMove);
        window.removeEventListener("pointerup", onUp);
      };
      window.addEventListener("pointermove", onMove);
      window.addEventListener("pointerup", onUp);
    };
  };

  const onResizePointerDown = createResizeHandler(indexHeight, setIndexHeight, MIN_INDEX_HEIGHT, MAX_INDEX_HEIGHT);
  const onThumbnailResizePointerDown = createResizeHandler(thumbnailHeight, setThumbnailHeight, MIN_THUMB_HEIGHT, MAX_THUMB_HEIGHT);

  // Actions
  const registryActions = {
    onSaveRegistryMeta: async (regId: number, meta: Partial<UserRegistryMeta>) => {
      await api.saveRegistryMeta(regId, meta);
      mutateRegistryMeta((prev) => {
        if (!prev) return prev;
        const sourceTypes = meta.source_types
          ? meta.source_types instanceof Set
            ? meta.source_types
            : new Set(meta.source_types)
          : prev.source_types;

        return {
          ...prev,
          ...meta,
          source_types: sourceTypes,
        };
      });
    },
    onSaveImageMeta: async (imageNumber: number, meta: Partial<UserImageMeta>) => {
      await api.saveImageMeta(registryId, imageNumber, meta);
      if (imageNumber === currentImage()) {
        mutateImageMeta((prev) => (prev ? { ...prev, ...meta } : prev));
      }
      mutateRegistryMeta((prev) => {
        if (!prev) return prev;
        const images = prev.images || [];
        const nextImages = [...images];
        const idx = imageNumber - 1;
        nextImages[idx] = { ...nextImages[idx], ...meta };
        return { ...prev, images: nextImages };
      });
    },
    onSaveAct: async (event: EventDetail) => {
      await api.saveAct(event);
      // Optional: Refresh eventRows if needed -> api.getEventRows(registryId).then(mutateEventRows)
    },
    onValidateAndNext: async (event: EventDetail) => {
      await api.saveAct(event);
    },
    onNewAct: () => console.info("Action: Create new act"),
    onReset: () => console.info("Action: Reset form"),
  };

  const renderIndex = (meta: RegistryMeta, isDetachedPanel: boolean) => (
    <Show when={!eventRows.error}>
      <IndexPanel
        visible={true}
        isDetached={isDetachedPanel}
        height={isDetachedPanel ? 0 : indexHeight()}
        rows={eventRows()!}
        selectedEventId={selectedEventId()}
        selectedEvent={selectedEventId() !== null ? getEventDetail(selectedEventId()!) : null}
        onToggle={isDetachedPanel ? handleCloseDetachedWindow : () => setIndexVisible(false)}
        onDetach={isDetachedPanel ? handleCloseDetachedWindow : () => detach({ title: `Index - ${meta.archive_reference}` })}
        onSelectRow={(row) => {
          setSelectedEventId(row.event_id);
          if (isDetachedPanel) sendMessage({ type: "SELECT_EVENT", id: row.event_id });
        }}
      />
    </Show>
  );

  return (
    <RegistryActionsProvider {...registryActions}>
      <Show
        when={!registryMeta.error}
        fallback={
          <div class="flex flex-col items-center justify-center w-full h-full text-danger bg-app">
            <Icon icon="lucide:alert-circle" class="w-12 h-12 mb-4" />
            <p class="text-[15px] font-medium">{t("registryViewer.loadingError")}</p>
          </div>
        }
      >
        <Show
          when={registryMeta()}
          fallback={
            <div class="flex flex-col items-center justify-center w-full h-full text-dim bg-app gap-3">
              <Icon icon="lucide:loader-2" class="animate-spin block" />
              <span class="text-[14px]">{t("registryViewer.loading")}</span>
            </div>
          }
        >
          <Show
            when={isDetachedMode}
            fallback={
              <div
                class="flex flex-col w-full h-full overflow-hidden bg-app text-main select-none antialiased"
                style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}
              >
                <TopBar
                  breadcrumbs={[<button onClick={() => openTab({ type: "home" })}>{t("home.title")}</button>, tabTile()]}
                  right={
                    <button
                      type="button"
                      onClick={() => setIndexVisible((v) => !v)}
                      class={[
                        "flex items-center gap-2 px-3 py-1.5 rounded-lg text-[13px] font-medium border transition-colors duration-100 flex-shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
                        indexVisible() ? "bg-accent-bg border-accent-border text-accent-text" : "bg-panel border-subtle text-muted hover:bg-tinted",
                      ].join(" ")}
                    >
                      <Icon icon="lucide:list" /> {t("registryViewer.index")} <Kbd>Ctrl I</Kbd>
                    </button>
                  }
                />

                <div class="flex flex-1 min-h-0 overflow-hidden">
                  <main class="flex-1 flex flex-col min-w-0 overflow-hidden">
                    <MainViewer
                      currentImage={currentImage()}
                      imageMeta={imageMeta()}
                      totalImages={registryMeta()!.total_images}
                      onImageChange={setCurrentImage}
                      registryId={registryId}
                    />
                    <div
                      class="flex-shrink-0 h-[6px] w-full cursor-row-resize z-10 group bg-app hover:bg-accent/25 active:bg-accent/50 transition-colors duration-150 flex items-center justify-center"
                      onPointerDown={onThumbnailResizePointerDown}
                      role="separator"
                      aria-orientation="horizontal"
                    >
                      <div class="flex flex-row gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
                        {[0, 1, 2].map(() => (
                          <div class="w-1 h-1 rounded-full bg-accent" />
                        ))}
                      </div>
                    </div>
                    <ThumbnailBar
                      height={thumbnailHeight()}
                      totalImages={registryMeta()!.total_images}
                      images={registryMeta()!.images ?? []}
                      currentImage={currentImage()}
                      onImageChange={setCurrentImage}
                      registryId={registryId}
                    />
                  </main>

                  <InfoNotesPanel registryMeta={registryMeta()!} imageMeta={imageMeta()} image={currentImage().toString()} />
                </div>

                {indexVisible() && !isDetached() && (
                  <div
                    class="flex-shrink-0 h-[6px] w-full cursor-row-resize z-10 group bg-active hover:bg-accent/25 active:bg-accent/50 transition-colors duration-150 flex items-center justify-center"
                    onPointerDown={onResizePointerDown}
                    role="separator"
                    aria-orientation="horizontal"
                  >
                    <div class="flex flex-row gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
                      {[0, 1, 2].map(() => (
                        <div class="w-1 h-1 rounded-full bg-accent" />
                      ))}
                    </div>
                  </div>
                )}

                {indexVisible() && !isDetached() && renderIndex(registryMeta()!, false)}

                <footer class="flex-shrink-0 flex items-center justify-between px-4 h-6 bg-panel border-t border-subtle" role="status">
                  <div class="flex items-center gap-4">
                    <span class="text-[11px] text-dim">
                      {currentImage()} / {registryMeta()!.total_images}
                    </span>
                    <span class="text-[11px] text-dim">
                      {t("registryViewer.viewsAndActs", { views: registryMeta()!.total_images, acts: eventRows()?.length || 0 })}
                    </span>
                  </div>
                  <div class="flex items-center gap-3">
                    <span class="text-[11px] text-dim tabular-nums">{new Date().toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" })}</span>
                  </div>
                </footer>
              </div>
            }
          >
            <div
              class="w-full h-full overflow-hidden flex flex-col bg-panel text-main antialiased"
              style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}
            >
              {renderIndex(registryMeta()!, true)}
            </div>
          </Show>
        </Show>
      </Show>
    </RegistryActionsProvider>
  );
};

export default ViewerPage;
