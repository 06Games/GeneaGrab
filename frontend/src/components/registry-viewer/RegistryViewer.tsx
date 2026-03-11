import { createSignal, createEffect, onCleanup, onMount } from "solid-js";
import { Kbd } from "../../ui/primitives";
import { TopBar } from "../../ui/TopBar";
import { MainViewer } from "./MainViewer";
import { ThumbnailBar } from "./ThumbnailBar";
import { InfoNotesPanel } from "./InfoNotesPanel";
import { IndexPanel } from "./index-panel/IndexPanel";
import { Icon } from "@iconify-icon/solid";
import { useDetachedWindow } from "../../hooks/DetachedWindow";
import { useI18n } from "../../ui/i18n";
import { RegistryActionsProvider } from "../../contexts/RegistryActionsContext";

import type { RegistryMeta, ImageMeta, EventRow, EventDetail, UserImageMeta } from "../../types/registry";

export interface RegistryViewerProps {
  registryMeta: RegistryMeta;
  imageMeta: ImageMeta;
  eventRows: EventRow[];
  initialImage?: number;
  onImageChange?: (image: number) => void;
  onSaveImageMeta?: (meta: Partial<UserImageMeta>) => Promise<void>;
  getEventDetail: (id: number) => EventDetail | null;
  onSaveAct?: (event: EventDetail) => void;
  onValidateAndNext?: (event: EventDetail) => void;
}

type SyncMessage =
  | { type: 'READY' }
  | { type: 'SYNC_STATE', selectedEventId: number | null }
  | { type: 'DETACHED_CLOSED' }
  | { type: 'TOGGLE_DETACHED' }
  | { type: 'SELECT_EVENT', id: number | null };

const MIN_INDEX_HEIGHT = 200;
const MAX_INDEX_HEIGHT = 700;
const DEFAULT_INDEX_HEIGHT = 340;

const MIN_THUMB_HEIGHT = 60;
const MAX_THUMB_HEIGHT = 200;
const DEFAULT_THUMB_HEIGHT = 125;

export const RegistryViewer = (props: RegistryViewerProps) => {
  const urlParams = new URLSearchParams(typeof window !== 'undefined' ? window.location.search : "");
  const isDetachedMode = urlParams.get('mode') === 'index';
  const activeRegistryId = urlParams.get('registryId') ? parseInt(urlParams.get('registryId')!, 10) : props.registryMeta.registry_id;

  const [currentImage, setCurrentImage] = createSignal(props.initialImage ?? 1);
  const [indexVisible, setIndexVisible] = createSignal(false);
  const [indexHeight, setIndexHeight] = createSignal(DEFAULT_INDEX_HEIGHT);
  const [thumbnailHeight, setThumbnailHeight] = createSignal(DEFAULT_THUMB_HEIGHT);
  const [selectedEventId, setSelectedEventId] = createSignal<number | null>(3);

  const { t } = useI18n();

  const { isDetached, setIsDetached, detach, closeSelf, sendMessage } = useDetachedWindow<SyncMessage>({
    id: `index-${activeRegistryId}`,
    title: `Index - ${props.registryMeta.archive_reference}`,
    queryParams: { mode: 'index', registryId: activeRegistryId.toString() },
    width: 1400,
    height: 600,
    onMessage: (msg) => {
      if (isDetachedMode) {
        if (msg.type === 'SYNC_STATE') setSelectedEventId(msg.selectedEventId);
      } else {
        if (msg.type === 'READY') {
          sendMessage({ type: 'SYNC_STATE', selectedEventId: selectedEventId() });
        }
        if (msg.type === 'DETACHED_CLOSED' || msg.type === 'TOGGLE_DETACHED') {
          setIsDetached(false);
        }
        if (msg.type === 'SELECT_EVENT') {
          setSelectedEventId(msg.id);
        }
      }
    }
  });

  const registryActions = {
    onSaveImageMeta: async (meta: Partial<UserImageMeta>) => {
      console.info("Action: Save image meta", meta);
      await props.onSaveImageMeta?.(meta);
    },
    onSaveAct: (event: EventDetail) => {
      console.info("Action: Save", event);
      props.onSaveAct?.(event);
    },
    onValidateAndNext: (event: EventDetail) => {
      console.info("Action: Validate", event);
      props.onValidateAndNext?.(event);
    },
    onNewAct: () => console.info("Action: Create new act"),
    onReset: () => console.info("Action: Reset form")
  };

  onMount(() => {
    if (isDetachedMode) {
      sendMessage({ type: 'READY' });
      const handleUnload = () => sendMessage({ type: 'DETACHED_CLOSED' });
      window.addEventListener('beforeunload', handleUnload);
      onCleanup(() => window.removeEventListener('beforeunload', handleUnload));
    }
  });

  createEffect(() => {
    if (!isDetachedMode) {
      sendMessage({ type: 'SYNC_STATE', selectedEventId: selectedEventId() });
    }
  });

  createEffect(() => {
    if (props.onImageChange) {
      props.onImageChange(currentImage());
    }
  });

  const selectedEvent = () => selectedEventId() !== null ? props.getEventDetail(selectedEventId()!) : null;

  const handleCloseDetachedWindow = async () => {
    sendMessage({ type: 'TOGGLE_DETACHED' });
    await closeSelf();
  };

  if (isDetachedMode) {
    return (
      <RegistryActionsProvider {...registryActions}>
        <div class="w-screen h-screen overflow-hidden flex flex-col bg-panel text-main antialiased" style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}>
          <IndexPanel
            visible={true}
            isDetached={true}
            height={0}
            rows={props.eventRows}
            selectedEventId={selectedEventId()}
            selectedEvent={selectedEvent()}
            onToggle={handleCloseDetachedWindow}
            onDetach={handleCloseDetachedWindow}
            onSelectRow={(row) => sendMessage({ type: 'SELECT_EVENT', id: row.event_id })}
          />
        </div>
      </RegistryActionsProvider>
    );
  }

  const onKeyDown = (e: KeyboardEvent) => {
    const target = e.target as HTMLElement;
    const inInput = target.tagName === "INPUT" || target.tagName === "TEXTAREA";
    if (!inInput) {
      if (e.key === "ArrowLeft") setCurrentImage(p => Math.max(1, p - 1));
      if (e.key === "ArrowRight") setCurrentImage(p => Math.min(props.registryMeta.total_images, p + 1));
    }
    if (e.key === "i" && e.ctrlKey) { e.preventDefault(); setIndexVisible(v => !v); }
  };

  onMount(() => window.addEventListener("keydown", onKeyDown));
  onCleanup(() => window.removeEventListener("keydown", onKeyDown));

  const onResizePointerDown = (e: PointerEvent) => {
    e.preventDefault();
    const startY = e.clientY;
    const startH = indexHeight();
    const onMove = (ev: PointerEvent) => {
      const delta = startY - ev.clientY;
      setIndexHeight(Math.max(MIN_INDEX_HEIGHT, Math.min(MAX_INDEX_HEIGHT, startH + delta)));
    };
    const onUp = () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  };

  const onThumbnailResizePointerDown = (e: PointerEvent) => {
    e.preventDefault();
    const startY = e.clientY;
    const startH = thumbnailHeight();
    const onMove = (ev: PointerEvent) => {
      const delta = startY - ev.clientY;
      setThumbnailHeight(Math.max(MIN_THUMB_HEIGHT, Math.min(MAX_THUMB_HEIGHT, startH + delta)));
    };
    const onUp = () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  };

  return (
    <RegistryActionsProvider {...registryActions}>
      <div class="flex flex-col w-screen h-screen overflow-hidden bg-app text-main select-none antialiased" style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}>
        
        <TopBar 
          breadcrumbs={[
            <a href="/">{t("home.title")}</a>,
            `${props.registryMeta.archive_reference} · ${props.registryMeta.places?.[0]} · ${Array.from(props.registryMeta.source_types).join(", ")}`
          ]}
          right={
            <button type="button" onClick={() => setIndexVisible(v => !v)} class={[
              "flex items-center gap-2 px-3 py-1.5 rounded-lg text-[13px] font-medium border transition-colors duration-100 flex-shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
              indexVisible() ? "bg-accent-bg border-accent-border text-accent-text" : "bg-panel border-subtle text-muted hover:bg-tinted",
            ].join(" ")}>
              <Icon icon="lucide:list" /> {t("registryViewer.index")} <Kbd>Ctrl I</Kbd>
            </button>
          }
        />

        <div class="flex flex-1 min-h-0 overflow-hidden">
          <main class="flex-1 flex flex-col min-w-0 overflow-hidden">
            <MainViewer currentImage={currentImage()} totalImages={props.registryMeta.total_images} onImageChange={setCurrentImage} registryId={props.registryMeta.registry_id} />
            <div class="flex-shrink-0 h-[6px] w-full cursor-row-resize z-10 group bg-app hover:bg-accent/25 active:bg-accent/50 transition-colors duration-150 flex items-center justify-center" onPointerDown={onThumbnailResizePointerDown} role="separator" aria-orientation="horizontal">
              <div class="flex flex-row gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
                {[0, 1, 2].map(() => <div class="w-1 h-1 rounded-full bg-accent" />)}
              </div>
            </div>
            <ThumbnailBar height={thumbnailHeight()} totalImages={props.registryMeta.total_images} currentImage={currentImage()} onImageChange={setCurrentImage} registryId={props.registryMeta.registry_id} />
          </main>

          <InfoNotesPanel
            registryMeta={props.registryMeta}
            imageMeta={props.imageMeta}
            image={currentImage().toString()}
          />
        </div>

        {indexVisible() && !isDetached() && (
          <div class="flex-shrink-0 h-[6px] w-full cursor-row-resize z-10 group bg-active hover:bg-accent/25 active:bg-accent/50 transition-colors duration-150 flex items-center justify-center" onPointerDown={onResizePointerDown} role="separator" aria-orientation="horizontal">
            <div class="flex flex-row gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
              {[0, 1, 2].map(() => <div class="w-1 h-1 rounded-full bg-accent" />)}
            </div>
          </div>
        )}

        {indexVisible() && !isDetached() && (
          <IndexPanel
            visible={true}
            isDetached={false}
            height={indexHeight()}
            rows={props.eventRows}
            selectedEventId={selectedEventId()}
            selectedEvent={selectedEvent()}
            onToggle={() => setIndexVisible(false)}
            onDetach={() => detach()}
            onSelectRow={(row) => setSelectedEventId(row.event_id)}
          />
        )}

        <footer class="flex-shrink-0 flex items-center justify-between px-4 h-6 bg-panel border-t border-subtle" role="status">
          <div class="flex items-center gap-4">
            <span class="text-[11px] text-dim">{currentImage()}</span>
            <span class="text-[11px] text-dim">{t("registryViewer.viewsAndActs", { views: props.registryMeta.total_images, acts: props.eventRows.length })}</span>
          </div>
          <div class="flex items-center gap-3">
            <span class="text-[11px] text-dim tabular-nums">
              {new Date().toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" })}
            </span>
          </div>
        </footer>
      </div>
    </RegistryActionsProvider>
  );
};

export default RegistryViewer;
