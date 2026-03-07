import { createSignal, For, Show } from "solid-js";
import type { RegistryMeta, ImageMeta, EventType } from "../../types/registry";
import { IconButton, MetaRow, ResizeHandle } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";

const ACT_TYPE_STYLES: Record<EventType, string> = {
  Naissance:      "text-[#2d6a4f] bg-[#d8f3dc] border-[#b7e4c7]",
  Mariage:        "text-[#1d4e89] bg-[#dbeafe] border-[#bfdbfe]",
  Décès:          "text-[#7f1d1d] bg-[#fee2e2] border-[#fecaca]",
  Sépulture:      "text-[#713f12] bg-[#ffedd5] border-[#fed7aa]",
  Testament:      "text-[#4b5563] bg-[#f3f4f6] border-[#e5e7eb]",
  Recensement:    "text-[#403937] bg-[#f2e9e4] border-[#e0d8cc]",
  Autre:          "text-[#6b6358] bg-[#f2ece3] border-[#e0d8cc]",
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
  const [registryExpanded, setRegistryExpanded] = createSignal(false);

  const saveInfo = () => ({
    saved:   { dot: "bg-[#3a8c5c]", label: "Enregistré" },
    saving:  { dot: "bg-[#b8743a] animate-pulse", label: "Enregistrement…" },
    error:   { dot: "bg-[#c0392b]", label: "Erreur" },
  })[props.saveStatus ?? "saved"];

  return (
    <aside
      class="flex flex-col w-72 min-w-[220px] flex-shrink-0 bg-white border-l border-[#e0d8cc] overflow-hidden"
      aria-label="Informations et notes"
    >
      <div class="flex items-center justify-between px-4 py-3 border-b border-[#e0d8cc] flex-shrink-0">
        <span class="text-[13px] font-semibold text-[#2c2820]">Registre</span>
        <IconButton title="Modifier les métadonnées" onClick={props.onEditRegistry}>
          <Icon icon="lucide:edit-2"></Icon>
        </IconButton>
      </div>

      {/* [A] Registry header — collapsible */}
      <div class="border-b border-[#e0d8cc] flex-shrink-0">
        <button
          type="button"
          onClick={() => setRegistryExpanded(e => !e)}
          aria-expanded={registryExpanded()}
          class="w-full flex items-center justify-between px-4 py-3 hover:bg-[#faf7f3] transition-colors duration-100 focus-visible:outline-none text-left"
        >
          <div class="overflow-hidden">
            <p class="text-[14px] font-medium text-[#2c2820] truncate">{props.registryMeta.archive_reference}</p>
            <p class="text-[12px] text-[#a89e93] truncate mt-0.5">
              {props.registryMeta.town} · {props.registryMeta.source_types?.size > 0 ? Array.from(props.registryMeta.source_types).join(", ") : "Inconnu"}
            </p>
          </div>
          <span class={["text-[#a89e93] flex-shrink-0 ml-2 inline-flex items-center justify-center transition-transform duration-150 origin-center", registryExpanded() ? "rotate-180" : ""].join(" ")}>
            <Icon icon="lucide:chevron-down" width="16" height="16" class="block" />
          </span>
        </button>

        <Show when={registryExpanded()}>
          <div class="px-4 pb-3 border-t border-[#f2ece3]">
            <div class="h-2" />
            <For each={Object.entries(props.registryMeta) as [keyof RegistryMeta, string][]}>
              {([key, value]) => <MetaRow label={key} value={value} />}
            </For>
          </div>
        </Show>
      </div>

      {/* [B] Image metadata */}
      <div class="px-4 py-3 border-b border-[#e0d8cc] flex-shrink-0">
        <div class="flex items-center justify-between mb-2">
          <span class="text-[13px] font-semibold text-[#2c2820]">Folio {props.image}</span>
        </div>

        <MetaRow label="Période"  value={props.imageMeta.dateRange} />
        <MetaRow label="Actes indexés"    value={String(Array.from(props.imageMeta.actTypes.values()).reduce((a, b) => a + b, 0))} />

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
          <label for="image-notes" class="text-[13px] font-semibold text-[#2c2820] cursor-pointer">
            Notes
          </label>
          <span class="text-[11px] text-[#a89e93] tabular-nums" aria-live="polite">
            {props.notes.length} car.
          </span>
        </div>

        <textarea
          id="image-notes"
          value={props.notes}
          onInput={(e) => props.onNotesChange(e.currentTarget.value)}
          placeholder="Annotations libres pour ce folio…"
          spellcheck={false}
          class={[
            "flex-1 resize-none rounded-lg border min-h-0",
            "bg-[#faf7f3] px-3 py-2.5",
            "text-[13px] text-[#2c2820] leading-relaxed",
            "placeholder:text-[#ccc4b8]",
            "border-[#e0d8cc] focus:border-[#b8743a]",
            "focus:ring-2 focus:ring-[#b8743a]/15 focus:outline-none",
            "transition-all duration-150",
            "scrollbar-thin scrollbar-thumb-[#e0d8cc]",
          ].join(" ")}
        />

        <div class="flex items-center gap-1.5 mt-2" aria-live="polite">
          <div class={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${saveInfo().dot}`} />
          <span class="text-[11px] text-[#a89e93]">{saveInfo().label}</span>
        </div>
      </div>
    </aside>
  );
};
