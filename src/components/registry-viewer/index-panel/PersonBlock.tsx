import { createSignal, Show } from "solid-js";
import type { PersonEntry } from "../../../types/registry";
import { ROLE_SUGGESTIONS, RELATION_SUGGESTIONS } from "../../../types/registry";
import { IconButton } from "../../../ui/primitives";
import { IndexField } from "./IndexField";

const ChevronDown = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor"><path d="M3 5l5 5 5-5H3Z" /></svg>
);
const TrashIcon = () => (
  <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="currentColor"><path d="M6 2h4v1h3v1H3V3h3V2Zm-2 3h8l-.8 9H6.8L4 5Zm2 2v5h1V7H6Zm3 0v5h1V7H9Z" /></svg>
);
const DragIcon = () => (
  <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="currentColor"><path d="M5 4a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm6 0a1 1 0 1 1 0-2 1 1 0 0 1 0 2ZM5 9a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm6 0a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm-6 5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Zm6 0a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z" /></svg>
);

const PROFESSION_OPTIONS = [
  "Laboureur", "Tisserand", "Notaire", "Charpentier", "Cordonnier", "Cultivateur", "Ménagère"
];

interface PersonBlockProps {
  person: PersonEntry;
  index: number;
  tabStart: number;
  onChange: (id: string, field: keyof PersonEntry, value: any) => void;
  onRemove: (id: string) => void;
}

export const PersonBlock = (props: PersonBlockProps) => {
  const [collapsed, setCollapsed] = createSignal(false);
  const p = () => props.person;

  const displayName = () => {
    const parts = [p().title, p().first_name, p().last_name].filter(Boolean);
    return parts.length > 0 ? parts.join(" ") : undefined;
  };

  const set = (field: keyof PersonEntry, value: any) =>
    props.onChange(p().person_id, field, value);

  return (
    <div class="border border-[#e0d8cc] rounded-xl overflow-hidden bg-white shadow-sm">
      {/* Header */}
      <div class="flex items-center px-3 py-2 gap-2 bg-[#faf7f3] border-b border-[#e0d8cc]">
        <span class="text-[#ccc4b8] cursor-grab select-none flex-shrink-0"><DragIcon /></span>

        <button
          type="button"
          onClick={() => setCollapsed(v => !v)}
          class="flex-1 flex items-center gap-2 text-left focus-visible:outline-none min-w-0"
        >
          <span class="text-[12px] font-semibold px-2 py-0.5 rounded-full bg-[#fef3e7] text-[#7a4a1e] border border-[#f0c990] flex-shrink-0">
            {p().role || <span class="italic text-[#a89e93]">Sans rôle</span>}
          </span>
          <Show when={displayName()}>
            <span class="text-[13px] text-[#6b6358] truncate">
              {p().is_deceased && <span class="text-xs mr-1 text-[#c0392b] font-bold">†</span>}
              {displayName()}
            </span>
          </Show>
        </button>

        <button type="button" onClick={() => setCollapsed(v => !v)} class="text-[#a89e93] hover:text-[#6b6358] focus-visible:outline-none">
          <span class={["transition-transform duration-150 block", collapsed() ? "-rotate-90" : ""].join(" ")}><ChevronDown /></span>
        </button>
        <IconButton title="Supprimer" onClick={() => props.onRemove(p().person_id)} class="text-[#c0392b] opacity-50 hover:opacity-100">
          <TrashIcon />
        </IconButton>
      </div>

      {/* Fields */}
      <Show when={!collapsed()}>
        <div class="px-3 pt-3 pb-3 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-4 gap-y-2">
          
          {/* Ligne 1 : Rôle & Identité */}
          <IndexField label="Rôle" value={p().role} tabIndex={props.tabStart} options={ROLE_SUGGESTIONS} onInput={v => set("role", v)} class="lg:col-span-1" />
          <IndexField label="Prénoms" value={p().first_name} tabIndex={props.tabStart + 1} onInput={v => set("first_name", v)} class="lg:col-span-1" />
          <IndexField label="Nom" value={p().last_name} tabIndex={props.tabStart + 2} onInput={v => set("last_name", v)} class="lg:col-span-1" />

          {/* Ligne 2 : Profil */}
          <IndexField label="Titre" value={p().title} tabIndex={props.tabStart + 3} placeholder="Me, Révérend..." onInput={v => set("title", v)} class="lg:col-span-1" />
          <IndexField label="Sexe" value={p().sex} tabIndex={props.tabStart + 4} options={["M", "F", "Inconnu"]} onInput={v => set("sex", v)} class="lg:col-span-1" />
          <div class="flex gap-2 lg:col-span-1">
            <IndexField label="Âge" value={p().age} tabIndex={props.tabStart + 5} onInput={v => set("age", v)} class="flex-1 min-w-0" />
            <IndexField label="Défunt(e)" type="checkbox" value={p().is_deceased} tabIndex={props.tabStart + 6} onInput={v => set("is_deceased", v)} class="w-[85px] flex-shrink-0" labelWidth="w-auto" />
          </div>

          {/* Ligne 3 : Lieux & Métier */}
          <IndexField label="Profession" value={p().occupation} tabIndex={props.tabStart + 7} options={PROFESSION_OPTIONS} defaultPinned onInput={v => set("occupation", v)} class="lg:col-span-1" />
          <IndexField label="Origine" value={p().origin_place} tabIndex={props.tabStart + 8} defaultPinned onInput={v => set("origin_place", v)} class="lg:col-span-1" />
          <IndexField label="Résidence" value={p().residence_place} tabIndex={props.tabStart + 9} defaultPinned onInput={v => set("residence_place", v)} class="lg:col-span-1" />

          {/* Ligne 4 : Liens de Parenté (Person Relationships) */}
          <IndexField label="Parenté" value={p().relationship_type} tabIndex={props.tabStart + 10} options={RELATION_SUGGESTIONS} placeholder="Lien..." onInput={v => set("relationship_type", v)} class="lg:col-span-1" />
          <IndexField label="Envers (Qui)" value={p().relationship_to} tabIndex={props.tabStart + 11} placeholder="Nom ou ID..." onInput={v => set("relationship_to", v)} class="lg:col-span-1" />
          <IndexField label="N° Ordre" value={p().sequence_number} tabIndex={props.tabStart + 12} onInput={v => set("sequence_number", v)} class="lg:col-span-1" />

          {/* Ligne 5 : Notes individuelles */}
          <IndexField label="Notes (Indiv.)" value={p().notes} tabIndex={props.tabStart + 13} onInput={v => set("notes", v)} class="md:col-span-2 lg:col-span-3" />
        </div>
      </Show>
    </div>
  );
};
