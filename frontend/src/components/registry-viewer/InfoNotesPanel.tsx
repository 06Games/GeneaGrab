import { createSignal, createEffect, For, onCleanup, Show } from "solid-js";
import { IconButton, MetaRow } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { useRegistryActions } from "../../contexts/RegistryActionsContext";
import { ImageMeta, UserImageMeta } from "../../types/image";
import { ActType, ActTypeCategory, RegistryMeta } from "../../types/registry";
import { EditRegistryModal } from "./EditRegistryModal";

const ACT_TYPE_STYLES: Record<ActTypeCategory, string> = {
  vital: "text-event-vital bg-event-vital-bg border-event-vital-border",
  union: "text-event-union bg-event-union-bg border-event-union-border",
  mortality: "text-event-mortality bg-event-mortality-bg border-event-mortality-border",
  census: "text-event-census bg-event-census-bg border-event-census-border",
  legal: "text-event-legal bg-event-legal-bg border-event-legal-border",
  land: "text-event-land bg-event-land-bg border-event-land-border",
  media: "text-event-media bg-event-media-bg border-event-media-border",
  military: "text-event-military bg-event-military-bg border-event-military-border",
  other: "text-event-other bg-event-other-bg border-event-other-border",
  unknown: "text-event-other bg-event-other-bg border-event-other-border",
};

interface InfoNotesPanelProps {
  registryMeta: RegistryMeta;
  imageMeta?: ImageMeta;
  image: string;
  onEditRegistry?: () => void;
}

export const InfoNotesPanel = (props: InfoNotesPanelProps) => {
  const { t } = useI18n();
  const actions = useRegistryActions();
  const [registryExpanded, setRegistryExpanded] = createSignal(false);
  const [saveStatus, setSaveStatus] = createSignal<"saved" | "unsaved" | "saving" | "error">("saved");
  const [isEditModalOpen, setIsEditModalOpen] = createSignal(false);
  const [copiedArk, setCopiedArk] = createSignal(false);

  // Helper accessors that normalize Set | Array | undefined to always return a safe Array
  const sourceTypesList = (): ActType[] => {
    const st = props.registryMeta?.source_types;
    if (!st) return [];
    if (Array.isArray(st)) return st;
    if (typeof (st as any)[Symbol.iterator] === "function") return Array.from(st);
    return [];
  };

  const placesList = (): (string | string[])[] => {
    const p = props.registryMeta?.places;
    if (!p) return [];
    if (Array.isArray(p)) return p;
    if (typeof (p as any)[Symbol.iterator] === "function") return Array.from(p);
    return [];
  };

  const collectionsList = (): string[] => {
    const c = props.registryMeta?.collection;
    if (!c) return [];
    if (Array.isArray(c)) return c;
    if (typeof (c as any)[Symbol.iterator] === "function") return Array.from(c);
    return [];
  };

  const copyArkUrl = async () => {
    if (props.registryMeta.ark_url) {
      try {
        await navigator.clipboard.writeText(props.registryMeta.ark_url);
        setCopiedArk(true);
        setTimeout(() => setCopiedArk(false), 2000);
      } catch (err) {
        console.error("Failed to copy ARK URL:", err);
      }
    }
  };

  const handleEditRegistry = () => {
    if (props.onEditRegistry) {
      props.onEditRegistry();
    } else {
      setIsEditModalOpen(true);
    }
  };

  const [name, setName] = createSignal("");
  const [dateRange, setDateRange] = createSignal("");
  const [notes, setNotes] = createSignal("");

  const saveInfo = () =>
    ({
      saved: { dot: "bg-success", label: t("infoPanel.saved") },
      unsaved: { dot: "bg-dim", label: t("infoPanel.unsaved") },
      saving: { dot: "bg-accent animate-pulse", label: t("infoPanel.saving") },
      error: { dot: "bg-danger", label: t("infoPanel.error") },
    })[saveStatus() ?? "saved"];

  let saveTimer: ReturnType<typeof setTimeout> | undefined;
  let pendingChanges: Partial<UserImageMeta> = {};
  let lastLoadedImageId: string | undefined;
  let isPristine = { name: true, date_range: true, notes: true };

  const flushSave = async (targetImageNum: number) => {
    if (saveTimer) {
      clearTimeout(saveTimer);
      saveTimer = undefined;
    }
    if (Object.keys(pendingChanges).length === 0) return;
    const toSend = { ...pendingChanges };
    pendingChanges = {};
    setSaveStatus("saving");
    try {
      await actions.onSaveImageMeta?.(targetImageNum, toSend);
      if (parseInt(props.image, 10) === targetImageNum && Object.keys(pendingChanges).length === 0) {
        setSaveStatus("saved");
      }
    } catch (err) {
      console.error("Failed to save image meta:", err);
      if (parseInt(props.image, 10) === targetImageNum) {
        setSaveStatus("error");
      }
    }
  };

  const queueSave = (meta: Partial<UserImageMeta>) => {
    setSaveStatus("unsaved");
    pendingChanges = { ...pendingChanges, ...meta };
    if (saveTimer) clearTimeout(saveTimer);
    const targetImageNum = parseInt(props.image, 10);
    saveTimer = setTimeout(() => {
      flushSave(targetImageNum);
    }, 800);
  };

  const handleNameInput = (val: string) => {
    isPristine.name = false;
    setName(val);
    queueSave({ name: val });
  };

  const handlePeriodInput = (val: string) => {
    isPristine.date_range = false;
    setDateRange(val);
    queueSave({ date_range: val });
  };

  const handleNotesInput = (val: string) => {
    isPristine.notes = false;
    setNotes(val);
    queueSave({ notes: val });
  };

  // Sync state when image changes or metadata arrives
  createEffect(() => {
    const currentImg = props.image;
    const meta = props.imageMeta;
    const currentImgNum = parseInt(currentImg, 10);

    if (currentImg !== lastLoadedImageId) {
      // Flush previous pending changes if any
      if (lastLoadedImageId !== undefined && Object.keys(pendingChanges).length > 0) {
        const prevNum = parseInt(lastLoadedImageId, 10);
        const sending = { ...pendingChanges };
        pendingChanges = {};
        actions.onSaveImageMeta?.(prevNum, sending).catch((err) => {
          console.error("Failed to save image meta on page change:", err);
        });
      }
      if (saveTimer) {
        clearTimeout(saveTimer);
        saveTimer = undefined;
      }
      lastLoadedImageId = currentImg;
      isPristine = { name: true, date_range: true, notes: true };
      setSaveStatus("saved");

      if (meta && meta.image_number === currentImgNum) {
        setName(meta.name ?? "");
        setDateRange(meta.date_range ?? "");
        setNotes(meta.notes ?? "");
      } else {
        setName("");
        setDateRange("");
        setNotes("");
      }
    } else if (meta && meta.image_number === currentImgNum) {
      // Image is the same, metadata updated (e.g. initial fetch resolved)
      if (isPristine.name) {
        setName(meta.name ?? "");
      }
      if (isPristine.date_range) {
        setDateRange(meta.date_range ?? "");
      }
      if (isPristine.notes) {
        setNotes(meta.notes ?? "");
      }
    }
  });

  onCleanup(() => {
    if (saveTimer) {
      clearTimeout(saveTimer);
      saveTimer = undefined;
    }
    if (Object.keys(pendingChanges).length > 0) {
      const targetImageNum = parseInt(props.image, 10);
      const sending = { ...pendingChanges };
      pendingChanges = {};
      actions.onSaveImageMeta?.(targetImageNum, sending).catch((err) => {
        console.error("Failed to save image meta on unmount:", err);
      });
    }
  });

  return (
    <aside
      class="flex flex-col w-72 min-w-[220px] max-w-full flex-shrink-0 bg-panel border-l border-subtle overflow-hidden select-text h-full"
      aria-label={t("infoPanel.ariaLabel")}
    >
      {/* Header */}
      <div class="flex items-center justify-between px-4 py-3 border-b border-subtle flex-shrink-0 min-w-0 select-none">
        <span class="text-[13px] font-semibold text-main truncate min-w-0">{t("infoPanel.title")}</span>
        <IconButton title={t("infoPanel.editMeta")} onClick={handleEditRegistry}>
          <Icon icon="lucide:edit-2"></Icon>
        </IconButton>
      </div>

      {/* Main Scrollable Body */}
      <div class="flex-1 overflow-y-auto overflow-x-hidden min-h-0 select-text scrollbar-thin scrollbar-thumb-subtle flex flex-col">
        {/* Collapsible Registry Summary & Details */}
        <div class="border-b border-subtle flex-shrink-0 min-w-0">
          <button
            type="button"
            onClick={() => setRegistryExpanded((e) => !e)}
            aria-expanded={registryExpanded()}
            class="w-full flex items-center justify-between px-4 py-3 hover:bg-tinted transition-colors duration-100 focus-visible:outline-none text-left min-w-0 overflow-hidden cursor-pointer select-none"
          >
            <div class="overflow-hidden min-w-0 flex-1 pr-2">
              <p class="text-[14px] font-semibold text-main truncate">
                {props.registryMeta.archive_reference || props.registryMeta.title || t("infoPanel.title")}
              </p>
              <p class="text-[12px] text-dim truncate mt-0.5">
                {placesList().length > 0
                  ? (Array.isArray(placesList()[0]) ? (placesList()[0] as string[]).join(", ") : String(placesList()[0]))
                  : t("infoPanel.unknown")}
                {" · "}
                {sourceTypesList().length > 0
                  ? sourceTypesList()
                      .map((st) => st.label || st.category)
                      .join(", ")
                  : t("infoPanel.unknown")}
              </p>
            </div>
            <span
              class={[
                "text-dim flex-shrink-0 inline-flex items-center justify-center transition-transform duration-150 origin-center",
                registryExpanded() ? "rotate-180" : "",
              ].join(" ")}
            >
              <Icon icon="lucide:chevron-down" width="16" height="16" class="block" />
            </span>
          </button>

          <Show when={registryExpanded()}>
            <div class="px-4 pb-4 pt-2 border-t border-subtle bg-panel flex flex-col gap-2.5 min-w-0 max-w-full">
              <Show when={props.registryMeta.title}>
                <div class="flex flex-col min-w-0 max-w-full">
                  <span class="text-[11px] font-semibold uppercase tracking-wider text-dim select-none">{t("infoPanel.titleLabel")}</span>
                  <p class="text-[13px] text-main font-medium mt-0.5 leading-snug break-words min-w-0" style={{ "overflow-wrap": "anywhere" }}>
                    {props.registryMeta.title}
                  </p>
                </div>
              </Show>

              <Show when={props.registryMeta.subtitle}>
                <div class="flex flex-col min-w-0 max-w-full">
                  <span class="text-[11px] font-semibold uppercase tracking-wider text-dim select-none">{t("infoPanel.subtitleLabel")}</span>
                  <p class="text-[12px] text-muted mt-0.5 leading-snug break-words min-w-0" style={{ "overflow-wrap": "anywhere" }}>
                    {props.registryMeta.subtitle}
                  </p>
                </div>
              </Show>

              <MetaRow label={t("infoPanel.archiveReference")} value={props.registryMeta.archive_reference} />

              <Show when={props.registryMeta.date_from || props.registryMeta.date_to}>
                <MetaRow
                  label={t("infoPanel.periodLabel")}
                  value={`${props.registryMeta.date_from ?? "?"} – ${props.registryMeta.date_to ?? "?"}`}
                />
              </Show>

              <Show when={props.registryMeta.author}>
                <MetaRow label={t("infoPanel.authorLabel")} value={props.registryMeta.author} />
              </Show>

              {/* Places */}
              <Show when={placesList().length > 0}>
                <div class="flex flex-col gap-1 pt-1 min-w-0 max-w-full overflow-hidden">
                  <span class="text-[11px] font-semibold uppercase tracking-wider text-dim select-none">{t("infoPanel.placesLabel")}</span>
                  <div class="flex flex-wrap gap-1.5 min-w-0 max-w-full">
                    <For each={placesList()}>
                      {(place) => {
                        const placeStr = Array.isArray(place) ? place.join(", ") : String(place);
                        return (
                          <span
                            class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md bg-tinted border border-subtle text-[11px] text-main max-w-full min-w-0 overflow-hidden"
                            title={placeStr}
                          >
                            <Icon icon="lucide:map-pin" width="11" height="11" class="text-dim flex-shrink-0" />
                            <span class="truncate min-w-0">{placeStr}</span>
                          </span>
                        );
                      }}
                    </For>
                  </div>
                </div>
              </Show>

              {/* Record types */}
              <Show when={sourceTypesList().length > 0}>
                <div class="flex flex-col gap-1 pt-1 min-w-0 max-w-full overflow-hidden">
                  <span class="text-[11px] font-semibold uppercase tracking-wider text-dim select-none">{t("infoPanel.typesLabel")}</span>
                  <div class="flex flex-wrap gap-1.5 min-w-0 max-w-full">
                    <For each={sourceTypesList()}>
                      {(type) => (
                        <span
                          class={[
                            "inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium border max-w-full min-w-0 truncate",
                            ACT_TYPE_STYLES[type.category] || "text-dim bg-tinted border-subtle",
                          ].join(" ")}
                          title={type.label || type.category}
                        >
                          <span class="truncate min-w-0">{type.label || type.category}</span>
                        </span>
                      )}
                    </For>
                  </div>
                </div>
              </Show>

              {/* Archival Hierarchy (Collection) */}
              <Show when={collectionsList().length > 0}>
                <div class="flex flex-col gap-1 pt-1 min-w-0 max-w-full">
                  <span class="text-[11px] font-semibold uppercase tracking-wider text-dim select-none">{t("infoPanel.collectionLabel")}</span>
                  <div class="flex flex-col gap-1.5 text-[12px] bg-tinted p-2.5 rounded-lg border border-subtle select-text">
                    <For each={collectionsList()}>
                      {(level, idx) => (
                        <div class="flex items-start gap-1.5 min-w-0 max-w-full">
                          <span class="text-[11px] text-dim font-mono flex-shrink-0 mt-0.5 select-none">{idx() + 1}.</span>
                          <span class="text-main font-medium break-words min-w-0 leading-snug" style={{ "overflow-wrap": "anywhere" }}>
                            {level}
                          </span>
                        </div>
                      )}
                    </For>
                  </div>
                </div>
              </Show>

              {/* Permanent Link / ARK */}
              <Show when={props.registryMeta.ark_url}>
                <div class="flex items-center justify-between pt-1 gap-2 min-w-0 max-w-full overflow-hidden">
                  <span class="text-[11px] font-medium text-dim truncate min-w-0 select-none">{t("infoPanel.arkUrlLabel")}</span>
                  <div class="flex items-center gap-1 flex-shrink-0">
                    <button
                      type="button"
                      onClick={copyArkUrl}
                      title={t("infoPanel.copyLink")}
                      class="px-2 py-0.5 rounded text-[11px] border border-subtle bg-tinted hover:bg-hover text-muted hover:text-main flex items-center gap-1 transition-colors cursor-pointer select-none"
                    >
                      <Icon icon={copiedArk() ? "lucide:check" : "lucide:copy"} width="11" height="11" />
                      <span>{copiedArk() ? t("infoPanel.copied") : t("infoPanel.copyLink")}</span>
                    </button>
                    <a
                      href={props.registryMeta.ark_url}
                      target="_blank"
                      rel="noopener noreferrer"
                      title={t("infoPanel.openLink")}
                      class="p-1 rounded text-muted hover:text-main hover:bg-hover transition-colors select-none"
                    >
                      <Icon icon="lucide:external-link" width="13" height="13" />
                    </a>
                  </div>
                </div>
              </Show>

              {/* General notes */}
              <Show when={props.registryMeta.notes}>
                <div class="flex flex-col gap-1 pt-1 min-w-0 max-w-full">
                  <span class="text-[11px] font-semibold uppercase tracking-wider text-dim select-none">{t("infoPanel.registryNotes")}</span>
                  <div
                    class="text-[12px] text-main bg-tinted p-2.5 rounded-lg border border-subtle whitespace-pre-wrap break-words leading-relaxed min-w-0 max-w-full cursor-text select-text"
                    style={{ "word-break": "break-word", "overflow-wrap": "anywhere" }}
                  >
                    {props.registryMeta.notes}
                  </div>
                </div>
              </Show>

              {/* Summary statistics */}
              <div class="flex items-center justify-between pt-2 mt-1 border-t border-subtle text-[11px] text-dim min-w-0 max-w-full select-none">
                <span class="truncate min-w-0">
                  {t("infoPanel.totalViews")}: <strong class="text-main font-medium">{props.registryMeta.total_images}</strong>
                </span>
                <span class="truncate min-w-0">
                  {t("infoPanel.totalActs")}: <strong class="text-main font-medium">{props.registryMeta.acts_count}</strong>
                </span>
              </div>

              {/* Edit metadata action button */}
              <button
                type="button"
                onClick={handleEditRegistry}
                class="w-full mt-1.5 py-1.5 px-3 rounded-lg border border-subtle hover:border-accent/40 bg-tinted hover:bg-accent-bg hover:text-accent-text text-muted text-[12px] font-medium flex items-center justify-center gap-1.5 transition-colors cursor-pointer min-w-0 select-none"
              >
                <Icon icon="lucide:edit-2" width="13" height="13" />
                <span>{t("infoPanel.editMeta")}</span>
              </button>
            </div>
          </Show>
        </div>

        {/* Image Metadata & Notes */}
        <Show when={props.imageMeta}>
          {/* Image Identifiers & Acts */}
          <div class="px-4 py-3 border-b border-subtle flex-shrink-0 min-w-0 max-w-full">
            <div class="flex items-center justify-between mb-2 min-w-0">
              <span class="text-[13px] font-semibold text-main truncate min-w-0 select-none">
                {name()
                  ? t("infoPanel.image.customName", { n: props.image, name: name() })
                  : t("infoPanel.image.default", { n: props.image })}
              </span>
            </div>

            <div class="flex flex-col gap-1.5 mb-2 min-w-0 max-w-full">
              <div class="grid grid-cols-[5.5rem_1fr] gap-x-2 items-center min-w-0 max-w-full overflow-hidden">
                <label for="image-name" class="text-[12px] text-dim truncate select-none cursor-pointer min-w-0">
                  {t("infoPanel.nameLabel")}
                </label>
                <input
                  id="image-name"
                  type="text"
                  value={name()}
                  onInput={(e) => handleNameInput(e.currentTarget.value)}
                  placeholder={t("infoPanel.namePlaceholder")}
                  class={[
                    "h-7 px-2.5 rounded-lg border text-[13px] text-main w-full min-w-0",
                    "bg-tinted border-subtle placeholder:text-subtle-md",
                    "focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none",
                    "transition-all duration-100",
                  ].join(" ")}
                />
              </div>

              <div class="grid grid-cols-[5.5rem_1fr] gap-x-2 items-center min-w-0 max-w-full overflow-hidden">
                <label for="image-period" class="text-[12px] text-dim truncate select-none cursor-pointer min-w-0">
                  {t("infoPanel.period")}
                </label>
                <input
                  id="image-period"
                  type="text"
                  value={dateRange()}
                  onInput={(e) => handlePeriodInput(e.currentTarget.value)}
                  placeholder={t("infoPanel.periodPlaceholder")}
                  class={[
                    "h-7 px-2.5 rounded-lg border text-[13px] text-main w-full min-w-0",
                    "bg-tinted border-subtle placeholder:text-subtle-md",
                    "focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none",
                    "transition-all duration-100",
                  ].join(" ")}
                />
              </div>
            </div>

            <MetaRow label={t("infoPanel.indexedLabel")} value={String(Array.from(props.imageMeta?.act_types?.values() ?? []).reduce((a, b) => a + b, 0))} />

            <div class="mt-2 flex flex-wrap gap-1.5 min-w-0 max-w-full">
              <For each={Array.from(props.imageMeta?.act_types?.entries() ?? [])}>
                {([type, count]) => (
                  <span
                    class={["inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium border max-w-full min-w-0 truncate", ACT_TYPE_STYLES[type.category]].join(" ")}
                    title={`${count}× ${type.label || type.category}`}
                  >
                    <span class="truncate">{count}× {type.label || type.category}</span>
                  </span>
                )}
              </For>
            </div>
          </div>

          {/* Image Notes Section */}
          <div class="flex-1 flex flex-col min-h-[180px] min-w-0 max-w-full px-4 pt-3 pb-4">
            <div class="flex items-center justify-between mb-2 min-w-0">
              <label for="image-notes" class="text-[13px] font-semibold text-main cursor-pointer truncate min-w-0 select-none">
                {t("infoPanel.notesLabel")}
              </label>
              <span class="text-[11px] text-dim tabular-nums flex-shrink-0 select-none" aria-live="polite">
                {notes().length} {t("infoPanel.charsShort")}
              </span>
            </div>

            <textarea
              id="image-notes"
              value={notes()}
              onInput={(e) => handleNotesInput(e.currentTarget.value)}
              placeholder={t("infoPanel.notesPlaceholder")}
              spellcheck={false}
              class={[
                "flex-1 min-h-[120px] resize-y rounded-lg border min-w-0 max-w-full",
                "bg-tinted px-3 py-2.5",
                "text-[13px] text-main leading-relaxed select-text",
                "placeholder:text-subtle-md",
                "border-subtle focus:border-accent",
                "focus:ring-2 focus:ring-accent/15 focus:outline-none",
                "transition-all duration-150",
                "scrollbar-thin scrollbar-thumb-subtle",
              ].join(" ")}
            />

            <div class="flex items-center gap-1.5 mt-2 min-w-0 flex-shrink-0 select-none" aria-live="polite">
              <div class={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${saveInfo().dot}`} />
              <span class="text-[11px] text-dim truncate">{saveInfo().label}</span>
            </div>
          </div>
        </Show>
      </div>

      <Show when={isEditModalOpen()}>
        <EditRegistryModal
          registryMeta={props.registryMeta}
          onClose={() => setIsEditModalOpen(false)}
          onSave={async (meta) => {
            await actions.onSaveRegistryMeta?.(props.registryMeta.id, meta);
          }}
        />
      </Show>
    </aside>
  );
};
