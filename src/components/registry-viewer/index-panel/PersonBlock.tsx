import { createSignal, Show } from "solid-js";
import type { PersonEntry } from "../../../types/registry";
import { RELATION_SUGGESTIONS } from "../../../types/registry";
import { IconButton } from "../../../ui/primitives";
import { IndexField } from "./IndexField";

const ChevronDown = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M3 5l5 5 5-5H3Z" />
  </svg>
);
const TrashIcon = () => (
  <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="currentColor">
    <path d="M6 2h4v1h3v1H3V3h3V2Zm-2 3h8l-.8 9H6.8L4 5Zm2 2v5h1V7H6Zm3 0v5h1V7H9Z" />
  </svg>
);
const DragIcon = () => (
  <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="currentColor">
    <path d="M5 4a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm6 0a1 1 0 1 1 0-2 1 1 0 0 1 0 2ZM5 9a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm6 0a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm-6 5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm6 0a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z" />
  </svg>
);

const PROFESSION_OPTIONS = [
  "Laboureur", "Tisserand", "Notaire", "Charpentier",
  "Cordonnier", "Épicier", "Instituteur", "Journalier",
  "Cultivateur", "Maréchal-ferrant", "Boulanger", "Vigneron",
];

interface PersonBlockProps {
  person: PersonEntry;
  index: number;
  tabStart: number;
  onChange: (id: string, field: keyof PersonEntry, value: string) => void;
  onRemove: (id: string) => void;
}

/**
 * PersonBlock — collapsible card for one person in an act.
 * The `relation` field is a free-form input with autocomplete suggestions.
 * No hardcoded roles — any text is valid.
 *
 * Wire onPointerDown on the drag handle to @thisbeyond/solid-dnd for reordering.
 */
export const PersonBlock = (props: PersonBlockProps) => {
  const [collapsed, setCollapsed] = createSignal(false);
  const p = () => props.person;

  const displayName = () => {
    const parts = [p().nom, p().prenoms].filter(Boolean);
    return parts.length > 0 ? parts.join(", ") : undefined;
  };

  const set = (field: keyof PersonEntry, value: string) =>
    props.onChange(p().id, field, value);

  return (
    <div class="border border-[#e0d8cc] rounded-xl overflow-hidden bg-white shadow-sm">
      {/* Header */}
      <div class="flex items-center px-3 py-2 gap-2 bg-[#faf7f3] border-b border-[#e0d8cc]">
        {/* Drag affordance — wire to DnD library */}
        <span class="text-[#ccc4b8] cursor-grab select-none flex-shrink-0" aria-hidden="true">
          <DragIcon />
        </span>

        {/* Relation chip + name */}
        <button
          type="button"
          onClick={() => setCollapsed(v => !v)}
          class="flex-1 flex items-center gap-2 text-left focus-visible:outline-none min-w-0"
          aria-expanded={!collapsed()}
        >
          <span class="text-[12px] font-semibold px-2 py-0.5 rounded-full bg-[#fef3e7] text-[#7a4a1e] border border-[#f0c990] flex-shrink-0">
            {p().relation || <span class="italic text-[#a89e93]">Sans relation</span>}
          </span>
          <Show when={displayName()}>
            <span class="text-[13px] text-[#6b6358] truncate">{displayName()}</span>
          </Show>
        </button>

        {/* Collapse toggle */}
        <button
          type="button"
          onClick={() => setCollapsed(v => !v)}
          class="text-[#a89e93] hover:text-[#6b6358] flex-shrink-0 focus-visible:outline-none"
          aria-label={collapsed() ? "Développer" : "Réduire"}
        >
          <span class={["transition-transform duration-150", collapsed() ? "-rotate-90" : ""].join(" ")}>
            <ChevronDown />
          </span>
        </button>

        {/* Remove */}
        <IconButton title="Supprimer cette personne" onClick={() => props.onRemove(p().id)} class="text-[#c0392b] opacity-50 hover:opacity-100">
          <TrashIcon />
        </IconButton>
      </div>

      {/* Fields */}
      <Show when={!collapsed()}>
        <div class="px-3 pt-3 pb-3 flex flex-col gap-2">
          {/* Relation — free-form with suggestions */}
          <IndexField
            label="Relation"
            value={p().relation}
            tabIndex={props.tabStart}
            options={RELATION_SUGGESTIONS}
            placeholder="Déclarant, Père, Témoin…"
            onInput={v => set("relation", v)}
          />
          <IndexField
            label="Nom"
            value={p().nom}
            tabIndex={props.tabStart + 1}
            onInput={v => set("nom", v)}
          />
          <IndexField
            label="Prénoms"
            value={p().prenoms}
            tabIndex={props.tabStart + 2}
            onInput={v => set("prenoms", v)}
          />
          <IndexField
            label="Âge / Né·e"
            value={p().ageOrBorn}
            tabIndex={props.tabStart + 3}
            placeholder="27 ans · 1765-04-12"
            onInput={v => set("ageOrBorn", v)}
          />
          <IndexField
            label="Profession"
            value={p().profession}
            tabIndex={props.tabStart + 4}
            options={PROFESSION_OPTIONS}
            defaultPinned
            onInput={v => set("profession", v)}
          />
          <IndexField
            label="Domicile"
            value={p().domicile}
            tabIndex={props.tabStart + 5}
            defaultPinned
            onInput={v => set("domicile", v)}
          />
        </div>
      </Show>
    </div>
  );
};
