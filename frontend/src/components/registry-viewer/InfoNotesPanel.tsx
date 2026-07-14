import { createSignal, For, onCleanup, Show } from "solid-js";
import { IconButton, MetaRow, ResizeHandle } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { useRegistryActions } from "../../contexts/RegistryActionsContext";
import { ImageMeta } from "../../types/image";
import { ActTypeCategory, RegistryMeta } from "../../types/registry";

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

  const saveInfo = () =>
    ({
      saved: { dot: "bg-success", label: t("infoPanel.saved") },
      unsaved: { dot: "bg-dim", label: t("infoPanel.unsaved") },
      saving: { dot: "bg-accent animate-pulse", label: t("infoPanel.saving") },
      error: { dot: "bg-danger", label: t("infoPanel.error") },
    })[saveStatus() ?? "saved"];

  let saveTimer: ReturnType<typeof setTimeout>;
  let lastMeta: Partial<ImageMeta> = {};

  const saveImageMeta = (meta: Partial<ImageMeta>) => {
    setSaveStatus("unsaved");
    clearTimeout(saveTimer);
    lastMeta = { ...lastMeta, ...meta }; // Accumulate changes to avoid multiple rapid saves
    saveTimer = setTimeout(async () => {
      setSaveStatus("saving");
      const success =
        (await actions
          .onSaveImageMeta?.(lastMeta)
          .catch(() => false)
          .then(() => true)) ?? false;
      setSaveStatus(success ? "saved" : "error");
    }, 800);
  };
  onCleanup(() => clearTimeout(saveTimer));

  return (
    <aside class="flex flex-col w-72 min-w-[220px] flex-shrink-0 bg-panel border-l border-subtle overflow-hidden" aria-label={t("infoPanel.ariaLabel")}>
      <div class="flex items-center justify-between px-4 py-3 border-b border-subtle flex-shrink-0">
        <span class="text-[13px] font-semibold text-main">{t("infoPanel.title")}</span>
        <IconButton title={t("infoPanel.editMeta")} onClick={props.onEditRegistry}>
          <Icon icon="lucide:edit-2"></Icon>
        </IconButton>
      </div>

      <div class="border-b border-subtle flex-shrink-0">
        <button
          type="button"
          onClick={() => setRegistryExpanded((e) => !e)}
          aria-expanded={registryExpanded()}
          class="w-full flex items-center justify-between px-4 py-3 hover:bg-tinted transition-colors duration-100 focus-visible:outline-none text-left"
        >
          <div class="overflow-hidden">
            <p class="text-[14px] font-medium text-main truncate">{props.registryMeta.archive_reference}</p>
            <p class="text-[12px] text-dim truncate mt-0.5">
              {props.registryMeta.places?.[0]?.join(", ") || t("infoPanel.unknown")} ·{" "}
              {props.registryMeta.source_types?.size > 0
                ? Array.from(props.registryMeta.source_types)
                    .map((source_type) => source_type.label)
                    .join(", ")
                : t("infoPanel.unknown")}
            </p>
          </div>
          <span
            class={[
              "text-dim flex-shrink-0 ml-2 inline-flex items-center justify-center transition-transform duration-150 origin-center",
              registryExpanded() ? "rotate-180" : "",
            ].join(" ")}
          >
            <Icon icon="lucide:chevron-down" width="16" height="16" class="block" />
          </span>
        </button>

        <Show when={registryExpanded()}>
          <div class="px-4 pb-3 border-t border-hover">
            <div class="h-2" />
            {/*TODO: Rework this.*/}
            <For each={Object.entries(props.registryMeta).filter(([_, v]) => typeof v === "string" || typeof v === "number") as [keyof RegistryMeta, string][]}>
              {([key, value]) => <MetaRow label={key} value={value} />}
            </For>
          </div>
        </Show>
      </div>

      <Show when={props.imageMeta}>
        {(imgMeta) => (
          <>
            <div class="px-4 py-3 border-b border-subtle flex-shrink-0">
              <div class="flex items-center justify-between mb-2">
                <span class="text-[13px] font-semibold text-main">
                  {imgMeta().name
                    ? t("infoPanel.image.customName", { n: props.image, name: imgMeta().name })
                    : t("infoPanel.image.default", { n: props.image })}
                </span>
              </div>

              <div class="flex flex-col gap-1.5 mb-2">
                <div class="grid grid-cols-[8rem_1fr] gap-x-3 items-center">
                  <label for="image-name" class="text-[12px] text-dim truncate select-none cursor-pointer">
                    {t("infoPanel.nameLabel")}
                  </label>
                  <input
                    id="image-name"
                    type="text"
                    value={imgMeta().name ?? ""}
                    onInput={(e) => saveImageMeta({ name: e.currentTarget.value })}
                    placeholder={t("infoPanel.namePlaceholder")}
                    class={[
                      "h-7 px-2.5 rounded-lg border text-[13px] text-main w-full min-w-0",
                      "bg-tinted border-subtle placeholder:text-subtle-md",
                      "focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none",
                      "transition-all duration-100",
                    ].join(" ")}
                  />
                </div>

                <div class="grid grid-cols-[8rem_1fr] gap-x-3 items-center">
                  <label for="image-period" class="text-[12px] text-dim truncate select-none cursor-pointer">
                    {t("infoPanel.period")}
                  </label>
                  <input
                    id="image-period"
                    type="text"
                    value={imgMeta().date_range ?? ""}
                    onInput={(e) => saveImageMeta({ date_range: e.currentTarget.value })}
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

              <MetaRow label={t("infoPanel.indexedLabel")} value={String(Array.from(imgMeta().act_types.values()).reduce((a, b) => a + b, 0))} />

              <div class="mt-2 flex flex-wrap gap-1.5">
                <For each={Array.from(imgMeta().act_types.entries())}>
                  {([type, count]) => (
                    <span
                      class={["inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium border", ACT_TYPE_STYLES[type.category]].join(" ")}
                    >
                      {count}× {type.label}
                    </span>
                  )}
                </For>
              </div>
            </div>

            <ResizeHandle />

            <div class="flex-1 flex flex-col min-h-0 px-4 pt-3 pb-3">
              <div class="flex items-center justify-between mb-2">
                <label for="image-notes" class="text-[13px] font-semibold text-main cursor-pointer">
                  {t("infoPanel.notesLabel")}
                </label>
                <span class="text-[11px] text-dim tabular-nums" aria-live="polite">
                  {imgMeta().notes?.length ?? 0} {t("infoPanel.charsShort")}
                </span>
              </div>

              <textarea
                id="image-notes"
                value={imgMeta().notes ?? ""}
                onInput={(e) => saveImageMeta({ notes: e.currentTarget.value })}
                placeholder={t("infoPanel.notesPlaceholder")}
                spellcheck={false}
                class={[
                  "flex-1 resize-none rounded-lg border min-h-0",
                  "bg-tinted px-3 py-2.5",
                  "text-[13px] text-main leading-relaxed",
                  "placeholder:text-subtle-md",
                  "border-subtle focus:border-accent",
                  "focus:ring-2 focus:ring-accent/15 focus:outline-none",
                  "transition-all duration-150",
                  "scrollbar-thin scrollbar-thumb-subtle",
                ].join(" ")}
              />

              <div class="flex items-center gap-1.5 mt-2" aria-live="polite">
                <div class={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${saveInfo().dot}`} />
                <span class="text-[11px] text-dim">{saveInfo().label}</span>
              </div>
            </div>
          </>
        )}
      </Show>
    </aside>
  );
};
