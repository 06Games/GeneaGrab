import { createSignal, createMemo, For, Show } from "solid-js";
import type { ActDetail, PersonEntry, ActType } from "../../../types/registry";
import { Button, Kbd, SectionLabel } from "../../../ui/primitives";
import { IndexField } from "./IndexField";
import { PersonBlock } from "./PersonBlock";

const ACT_TYPE_OPTIONS: ActType[] = [
  "Naissance", "Mariage", "Décès", "Reconnaissance", "Autre",
];

const PlusIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M7 7V3h2v4h4v2H9v4H7V9H3V7h4Z" />
  </svg>
);

interface DetailZoneProps {
  act: ActDetail | null;
  /** Ref callback so parent can focus the first input */
  onRef?: (el: HTMLDivElement) => void;
  /** Called when Tab-back from first field — returns focus to grid */
  onFocusGrid?: () => void;
  onSave?: (act: ActDetail) => void;
  onValidateAndNext?: (act: ActDetail) => void;
  onReset?: () => void;
}

/**
 * DetailZone
 *
 * Keyboard focus contract:
 *  - When focused from GlobalGrid (→ or Tab), the first input gains focus.
 *  - Shift+Tab on the first input fires onFocusGrid to return to the list.
 *  - Tab cycles through all inputs in tabIndex order.
 *  - Ctrl+S saves, Enter on last field validates & advances.
 *
 * People are a dynamic list — no hardcoded roles.
 * Each PersonBlock gets a free-form `relation` autocomplete field.
 *
 * Tab index layout:
 *   1–9   Act-level fields
 *   10+   PersonBlock fields in 10-unit steps (6 fields each)
 */
export const DetailZone = (props: DetailZoneProps) => {
  const [people, setPeople] = createSignal<PersonEntry[]>(
    props.act?.people ?? []
  );

  let firstInputRef!: HTMLInputElement;

  // Expose focus method via ref
  const setRef = (el: HTMLDivElement) => {
    props.onRef?.(el);
  };

  const addPerson = () => {
    const newPerson: PersonEntry = {
      id: crypto.randomUUID(),
      relation: "", nom: "", prenoms: "",
      ageOrBorn: "", profession: "", domicile: "",
    };
    setPeople(prev => [...prev, newPerson]);
  };

  const updatePerson = (id: string, field: keyof PersonEntry, value: string) => {
    setPeople(prev => prev.map(p => p.id === id ? { ...p, [field]: value } : p));
  };

  const removePerson = (id: string) => {
    setPeople(prev => prev.filter(p => p.id !== id));
  };

  // Shift+Tab on first field goes back to grid
  const handleFirstFieldKeyDown = (e: KeyboardEvent) => {
    if (e.key === "Tab" && e.shiftKey) {
      e.preventDefault();
      props.onFocusGrid?.();
    }
  };

  return (
    <div
      ref={setRef}
      class="flex-1 flex flex-col min-w-0 overflow-hidden bg-[#faf7f3]"
    >
      {/* Header */}
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-b border-[#e0d8cc] bg-white">
        <div class="flex items-center gap-2 overflow-hidden">
          <span class="text-[13px] font-semibold text-[#2c2820] flex-shrink-0">
            {props.act ? `Acte #${props.act.id}` : "Aucun acte sélectionné"}
          </span>
          <Show when={props.act}>
            <span class="text-[12px] text-[#a89e93] truncate">— {props.act!.date}</span>
          </Show>
        </div>
        {/* Keyboard hints */}
        <div class="flex items-center gap-1.5 flex-shrink-0 text-[11px] text-[#a89e93]">
          <Kbd>←</Kbd> liste
          <span class="mx-1">·</span>
          <Kbd>Ctrl S</Kbd> sauvegarder
        </div>
      </div>

      {/* Scrollable form */}
      <div class="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-5 scrollbar-thin scrollbar-thumb-[#e0d8cc]">

        <Show when={!props.act}>
          <div class="flex-1 flex items-center justify-center py-12">
            <p class="text-[14px] text-[#a89e93] text-center">
              Sélectionnez un acte dans la liste<br />
              <span class="text-[12px]">ou appuyez sur <Kbd>N</Kbd> pour en créer un nouveau</span>
            </p>
          </div>
        </Show>

        <Show when={props.act}>
          {/* Act-level fields */}
          <section>
            <SectionLabel>Acte</SectionLabel>
            <div class="grid grid-cols-2 gap-x-6 gap-y-2">
              <IndexField
                label="Date"
                value={props.act!.date}
                tabIndex={1}
                placeholder="JJ Mois AAAA"
                onInput={() => {}}
              />
              <IndexField
                label="Type"
                value={props.act!.type}
                tabIndex={2}
                options={ACT_TYPE_OPTIONS}
              />
              <IndexField
                label="Acte n°"
                value={props.act!.actNumber}
                tabIndex={3}
                placeholder="47"
              />
              <IndexField
                label="Folio"
                value={props.act!.folioRef}
                tabIndex={4}
                defaultPinned
              />
            </div>
          </section>

          {/* People */}
          <section>
            <SectionLabel>Personnes</SectionLabel>
            <div class="flex flex-col gap-2">
              <Index each={people()}>
                {(person, i) => (
                  <PersonBlock
                    person={person}
                    index={i}
                    tabStart={10 + i * 10}
                    onChange={updatePerson}
                    onRemove={removePerson}
                  />
                )}
              </Index>
            </div>

            <button
              type="button"
              onClick={addPerson}
              class={[
                "mt-2 w-full py-2 rounded-xl border-2 border-dashed border-[#e0d8cc]",
                "text-[13px] text-[#a89e93] flex items-center justify-center gap-1.5",
                "hover:text-[#6b6358] hover:border-[#ccc4b8] hover:bg-white",
                "transition-colors duration-100",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#b8743a]",
              ].join(" ")}
            >
              <PlusIcon /> Ajouter une personne
            </button>
          </section>

          {/* Remarks */}
          <section>
            <SectionLabel>Remarques</SectionLabel>
            <textarea
              rows={3}
              value={props.act!.remarks}
              placeholder="Remarques sur l'acte…"
              spellcheck={false}
              class={[
                "w-full resize-none rounded-lg border",
                "bg-white px-3 py-2.5 text-[13px] text-[#2c2820] leading-relaxed",
                "placeholder:text-[#ccc4b8]",
                "border-[#e0d8cc] focus:border-[#b8743a]",
                "focus:ring-2 focus:ring-[#b8743a]/15 focus:outline-none",
                "transition-all duration-150",
              ].join(" ")}
            />
          </section>
        </Show>
      </div>

      {/* Action bar */}
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-t border-[#e0d8cc] bg-white">
        <Button variant="ghost" size="sm" onClick={props.onReset}>
          Réinitialiser
        </Button>
        <div class="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => props.act && props.onSave?.({ ...props.act, people: people() })}
            disabled={!props.act}
          >
            Sauvegarder <Kbd>Ctrl S</Kbd>
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => props.act && props.onValidateAndNext?.({ ...props.act, people: people() })}
            disabled={!props.act}
          >
            Valider & Suivant <Kbd class="bg-[#8a5020] text-white border-[#8a5020]">↵</Kbd>
          </Button>
        </div>
      </div>
    </div>
  );
};
