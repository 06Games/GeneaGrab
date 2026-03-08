import { createSignal, For, Show } from "solid-js";
import type { RegistryMeta, ImageMeta, EventType } from "../../types/registry";
import { IconButton, MetaRow, ResizeHandle } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";

const ACT_TYPE_STYLES: Record<EventType, string> = {
  Birth: "text-event-birth bg-event-birth-bg border-event-birth-border",
  Marriage: "text-event-marriage bg-event-marriage-bg border-event-marriage-border",
  Death: "text-event-death bg-event-death-bg border-event-death-border",
  Burial: "text-event-burial bg-event-burial-bg border-event-burial-border",
  Census: "text-event-census bg-event-census-bg border-event-census-border",
  Notarial: "text-event-notarial bg-event-notarial-bg border-event-notarial-border",
  Other: "text-event-other bg-event-other-bg border-event-other-border",
};

interface InfoNotesPanelProps {
  registryMeta: RegistryMeta;
  imageMeta: ImageMeta;
  image: string;
  notes: string;
  onNotesChange: (value: string) => void;
  onEditRegistry?: () => void;
  saveStatus?: "saved" | "saving" | "error";
}

export const InfoNotesPanel = (props: InfoNotesPanelProps) => {
  const { t } = useI18n();
  const [registryExpanded, setRegistryExpanded] = createSignal(false);

  const saveInfo = () => ({
    saved: { dot: "bg-success", label: t("infoPanel.saved") },
    saving: { dot: "bg-accent animate-pulse", label: t("infoPanel.saving") },
    error: { dot: "bg-danger", label: t("infoPanel.error") },
  })[props.saveStatus ?? "saved"];

  return (
    <aside
      class="flex flex-col w-72 min-w-[220px] flex-shrink-0 bg-panel border-l border-subtle overflow-hidden"
      aria-label={t("infoPanel.ariaLabel")}
    >
      <div class="flex items-center justify-between px-4 py-3 border-b border-subtle flex-shrink-0">
        <span class="text-[13px] font-semibold text-main">{t("infoPanel.title")}</span>
        <IconButton title={t("infoPanel.editMeta")} onClick={props.onEditRegistry}>
          <Icon icon="lucide:edit-2"></Icon>
        </IconButton>
      </div>

      {/* [A] Registry header — collapsible */}
      <div class="border-b border-subtle flex-shrink-0">
        <button
          type="button"
          onClick={() => setRegistryExpanded(e => !e)}
          aria-expanded={registryExpanded()}
          class="w-full flex items-center justify-between px-4 py-3 hover:bg-tinted transition-colors duration-100 focus-visible:outline-none text-left"
        >
          <div class="overflow-hidden">
            <p class="text-[14px] font-medium text-main truncate">{props.registryMeta.archive_reference}</p>
            <p class="text-[12px] text-dim truncate mt-0.5">
              {props.registryMeta.town} · {props.registryMeta.source_types?.size > 0 ? Array.from(props.registryMeta.source_types).join(", ") : t("infoPanel.unknown")}
            </p>
          </div>
          <span class={["text-dim flex-shrink-0 ml-2 inline-flex items-center justify-center transition-transform duration-150 origin-center", registryExpanded() ? "rotate-180" : ""].join(" ")}>
            <Icon icon="lucide:chevron-down" width="16" height="16" class="block" />
          </span>
        </button>

        <Show when={registryExpanded()}>
          <div class="px-4 pb-3 border-t border-hover">
            <div class="h-2" />
            <For each={Object.entries(props.registryMeta) as [keyof RegistryMeta, string][]}>
              {([key, value]) => <MetaRow label={key} value={value} />}
            </For>
          </div>
        </Show>
      </div>

      {/* [B] Image metadata */}
      <div class="px-4 py-3 border-b border-subtle flex-shrink-0">
        <div class="flex items-center justify-between mb-2">
          <span class="text-[13px] font-semibold text-main">{
            props.imageMeta.userMeta.name
              ? t("infoPanel.image.customName", { n: props.image, name: props.imageMeta.userMeta.name })
              : t("infoPanel.image.default", { n: props.image })
          }</span>
        </div>

        <MetaRow label={t("infoPanel.period")} value={props.imageMeta.userMeta.dateRange} />
        <MetaRow label={t("infoPanel.indexedLabel")} value={String(Array.from(props.imageMeta.actTypes.values()).reduce((a, b) => a + b, 0))} />

        <div class="mt-2 flex flex-wrap gap-1.5">
          <For each={Array.from(props.imageMeta.actTypes.entries())}>
            {([type, count]) => {
              return (
                <span class={[
                  "inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium border",
                  ACT_TYPE_STYLES[type],
                ].join(" ")}>
                  {count}× {type}
                </span>
              );
            }}
          </For>
        </div>
      </div>

      {/* Drag handle */}
      <ResizeHandle />

      {/* [C] Notes — flex-grow */}
      <div class="flex-1 flex flex-col min-h-0 px-4 pt-3 pb-3">
        <div class="flex items-center justify-between mb-2">
          <label for="image-notes" class="text-[13px] font-semibold text-main cursor-pointer">
            {t("infoPanel.notesLabel")}
          </label>
          <span class="text-[11px] text-dim tabular-nums" aria-live="polite">
            {props.notes.length} {t("infoPanel.charsShort")}
          </span>
        </div>

        <textarea
          id="image-notes"
          value={props.notes}
          onInput={(e) => props.onNotesChange(e.currentTarget.value)}
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
    </aside>
  );
};
