import { createSignal, Show, Index } from "solid-js";
import type { EventDetail, PersonEntry, EventType } from "../../../types/registry";
import { Button, Kbd, SectionLabel } from "../../../ui/primitives";
import { IndexField } from "./IndexField";
import { PersonBlock } from "./PersonBlock";
import { Icon } from "@iconify-icon/solid";

const EVENT_TYPE_OPTIONS: string[] = [
  "Naissance", "Mariage", "Décès", "Sépulture", "Testament", "Recensement", "Autre",
];

interface DetailZoneProps {
  event: EventDetail | null;
  onRef?: (el: HTMLDivElement) => void;
  onFocusGrid?: () => void;
  onSave?: (event: EventDetail) => void;
  onValidateAndNext?: (event: EventDetail) => void;
  onReset?: () => void;
}

export const DetailZone = (props: DetailZoneProps) => {
  const [people, setPeople] = createSignal<PersonEntry[]>(
    props.event?.people ?? []
  );

  const setRef = (el: HTMLDivElement) => { props.onRef?.(el); };

  const addPerson = () => {
    const newPerson: PersonEntry = {
      person_id: crypto.randomUUID(),
      role: "", first_name: "", last_name: "", sex: "", title: "", age: "",
      is_deceased: false, occupation: "", origin_place: "", residence_place: "",
      sequence_number: "", notes: "", relationship_type: "", relationship_to: ""
    };
    setPeople(prev => [...prev, newPerson]);
  };

  const updatePerson = (id: string, field: keyof PersonEntry, value: any) => {
    setPeople(prev => prev.map(p => p.person_id === id ? { ...p, [field]: value } : p));
  };

  const removePerson = (id: string) => {
    setPeople(prev => prev.filter(p => p.person_id !== id));
  };

  const setEventField = (field: keyof EventDetail, val: string) => {
     // Local state mutation strategy would go here, assuming parent passes down a signal
     // Or we emit changes via a callback. Simplified for view.
  };

  return (
    <div ref={setRef} class="flex-1 flex flex-col min-w-0 overflow-hidden bg-[#faf7f3]">
      {/* Header */}
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-b border-[#e0d8cc] bg-white">
        <div class="flex items-center gap-2 overflow-hidden">
          <span class="text-[13px] font-semibold text-[#2c2820] flex-shrink-0">
            {props.event ? `Acte / Événement #${props.event.event_id}` : "Aucun acte sélectionné"}
          </span>
          <Show when={props.event}>
            <span class="text-[12px] text-[#a89e93] truncate">— {props.event!.title || props.event!.date}</span>
          </Show>
        </div>
        <div class="flex items-center gap-1.5 flex-shrink-0 text-[11px] text-[#a89e93]">
          <Kbd>←</Kbd> liste <span class="mx-1">·</span> <Kbd>Ctrl S</Kbd> sauvegarder
        </div>
      </div>

      {/* Formulaire scrollable */}
      <div class="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-5 scrollbar-thin scrollbar-thumb-[#e0d8cc]">

        <Show when={!props.event}>
          <div class="flex-1 flex items-center justify-center py-12">
            <p class="text-[14px] text-[#a89e93] text-center">
              Sélectionnez un acte dans la liste<br />
              <span class="text-[12px]">ou appuyez sur <Kbd>N</Kbd> pour en créer un nouveau</span>
            </p>
          </div>
        </Show>

        <Show when={props.event}>
          {/* Section Acte (events) */}
          <section>
            <SectionLabel>Détails de l'Acte</SectionLabel>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-2">
              <IndexField label="Type" value={props.event!.event_type} tabIndex={1} options={EVENT_TYPE_OPTIONS} />
              <IndexField label="Titre" value={props.event!.title} tabIndex={2} placeholder="Testament de..." />
              <IndexField label="N° Acte" value={props.event!.act_number} tabIndex={3} placeholder="Ex: 47" />
              
              <IndexField label="Date (Texte)" value={props.event!.date} tabIndex={4} placeholder="12 Floréal an III" />
              <IndexField label="Date Norm." value={props.event!.date_normalized} tabIndex={5} placeholder="YYYY-MM-DD" />
              <IndexField label="Page/Folio" value={props.event!.page} tabIndex={6} />
              
              <IndexField label="Ville" value={props.event!.town} tabIndex={7} defaultPinned />
              <IndexField label="Paroisse" value={props.event!.parish} tabIndex={8} defaultPinned />
              <IndexField label="Hameau" value={props.event!.hamlet} tabIndex={9} defaultPinned />
              
              <IndexField label="N° Image (Vue)" value={props.event!.image_number} tabIndex={10} />
            </div>
          </section>

          {/* Section Personnes (persons, event_participants, person_relationships) */}
          <section>
            <SectionLabel>Personnes & Participants</SectionLabel>
            <div class="flex flex-col gap-2">
              <Index each={people()}>
                {(person, i) => (
                  <PersonBlock
                    person={person()}
                    index={i}
                    tabStart={20 + i * 20} // Laisse 20 tabIndex libres par personne
                    onChange={updatePerson}
                    onRemove={removePerson}
                  />
                )}
              </Index>
            </div>

            <button
              type="button" onClick={addPerson}
              class="mt-2 w-full py-2 rounded-xl border-2 border-dashed border-[#e0d8cc] text-[13px] text-[#a89e93] flex items-center justify-center gap-1.5 hover:text-[#6b6358] hover:border-[#ccc4b8] hover:bg-white transition-colors duration-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#b8743a]"
            >
              <Icon icon="lucide:plus" width="16" height="16" class="block" /> Ajouter une personne
            </button>
          </section>

          {/* Section Textes et Notes */}
          <section class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <SectionLabel>Transcription</SectionLabel>
              <textarea
                rows={4} value={props.event!.transcription_text} placeholder="Texte intégral de l'acte..."
                spellcheck={false} class="w-full resize-y rounded-lg border bg-white px-3 py-2.5 text-[13px] text-[#2c2820] border-[#e0d8cc] focus:border-[#b8743a] focus:ring-2 focus:ring-[#b8743a]/15 outline-none"
              />
            </div>
            <div>
              <SectionLabel>Notes de l'Acte</SectionLabel>
              <textarea
                rows={4} value={props.event!.notes} placeholder="Remarques de l'indexeur..."
                spellcheck={false} class="w-full resize-y rounded-lg border bg-white px-3 py-2.5 text-[13px] text-[#2c2820] border-[#e0d8cc] focus:border-[#b8743a] focus:ring-2 focus:ring-[#b8743a]/15 outline-none"
              />
            </div>
          </section>
        </Show>
      </div>

      {/* Action bar */}
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-t border-[#e0d8cc] bg-white">
        <Button variant="ghost" size="sm" onClick={props.onReset}>Réinitialiser</Button>
        <div class="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => props.event && props.onSave?.({ ...props.event, people: people() })} disabled={!props.event}>
            Sauvegarder <Kbd>Ctrl S</Kbd>
          </Button>
          <Button variant="primary" size="sm" onClick={() => props.event && props.onValidateAndNext?.({ ...props.event, people: people() })} disabled={!props.event}>
            Valider & Suivant <Kbd>↵</Kbd>
          </Button>
        </div>
      </div>
    </div>
  );
};
