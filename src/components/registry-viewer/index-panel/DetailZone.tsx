import { createSignal, Show, Index } from "solid-js";
import type { EventDetail, PersonEntry } from "../../../types/registry";
import { EVENT_TYPE_OPTIONS } from "../../../types/registry";
import { Button, Kbd, SectionLabel } from "../../../ui/primitives";
import { IndexField } from "./IndexField";
import { PersonBlock } from "./PersonBlock";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../../ui/i18n";

interface DetailZoneProps {
  event: EventDetail | null;
  onRef?: (el: HTMLDivElement) => void;
  onFocusGrid?: () => void;
  onSave?: (event: EventDetail) => void;
  onValidateAndNext?: (event: EventDetail) => void;
  onReset?: () => void;
}

export const DetailZone = (props: DetailZoneProps) => {
  const { t } = useI18n();
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

  return (
    <div ref={setRef} class="flex-1 flex flex-col min-w-0 overflow-hidden bg-tinted">
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-b border-subtle bg-panel">
        <div class="flex items-center gap-2 overflow-hidden">
          <span class="text-[13px] font-semibold text-main flex-shrink-0">
            {props.event ? t("detail.headerTitle", { id: props.event.event_id }) : t("detail.noSelection")}
          </span>
          <Show when={props.event}>
            <span class="text-[12px] text-dim truncate">{props.event!.title || props.event!.date }</span>
          </Show>
        </div>
        <div class="flex items-center gap-1.5 flex-shrink-0 text-[11px] text-dim">
          <Kbd>←</Kbd> {t("detail.actions.list")} <span class="mx-1">·</span> <Kbd>Ctrl S</Kbd> {t("detail.actions.save")}
        </div>
      </div>

      <div class="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-5 scrollbar-thin scrollbar-thumb-subtle">
        <Show when={!props.event}>
          <div class="flex-1 flex items-center justify-center py-12">
            <p class="text-[14px] text-dim text-center">
              {t("detail.emptyPrompt")}<br />
              <span class="text-[12px]"><span>{t("detail.emptyPrompt.prefix")}</span> <Kbd>N</Kbd> <span>{t("detail.emptyPrompt.suffix")}</span></span>
            </p>
          </div>
        </Show>

        <Show when={props.event}>
          <section>
            <SectionLabel>{t("detail.sections.details")}</SectionLabel>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-2">
              <IndexField label={t("detail.labels.type")} value={props.event!.event_type} tabIndex={1} options={EVENT_TYPE_OPTIONS} />
              <IndexField label={t("detail.labels.title")} value={props.event!.title} tabIndex={2} placeholder={t("detail.placeholders.title")} />
              <IndexField label={t("detail.labels.actNumber")} value={props.event!.act_number} tabIndex={3} placeholder={t("detail.placeholders.actNumber")} />
              <IndexField label={t("detail.labels.dateText")} value={props.event!.date} tabIndex={4} placeholder={t("detail.placeholders.dateText")} />
              <IndexField label={t("detail.labels.dateNorm")} value={props.event!.date_normalized} tabIndex={5} placeholder={t("detail.placeholders.dateNorm")} />
              <IndexField label={t("detail.labels.pageFolio")} value={props.event!.page} tabIndex={6} />
              <IndexField label={t("detail.labels.town")} value={props.event!.town} tabIndex={7} defaultPinned />
              <IndexField label={t("detail.labels.parish")} value={props.event!.parish} tabIndex={8} defaultPinned />
              <IndexField label={t("detail.labels.hamlet")} value={props.event!.hamlet} tabIndex={9} defaultPinned />
              <IndexField label={t("detail.labels.imageNumber")} value={props.event!.image_number} tabIndex={10} />
            </div>
          </section>

          <section>
            <SectionLabel>{t("detail.sections.people")}</SectionLabel>
            <div class="flex flex-col gap-2">
              <Index each={people()}>
                {(person, i) => (
                  <PersonBlock
                    person={person()}
                    index={i}
                    tabStart={20 + i * 20} 
                    onChange={updatePerson}
                    onRemove={removePerson}
                  />
                )}
              </Index>
            </div>

            <button
              type="button" onClick={addPerson}
              class="mt-2 w-full py-2 rounded-xl border-2 border-dashed border-subtle text-[13px] text-dim flex items-center justify-center gap-1.5 hover:text-muted hover:border-subtle-md hover:bg-panel transition-colors duration-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              <Icon icon="lucide:plus" width="16" height="16" class="block" /> {t("detail.addPerson")}
            </button>
          </section>

          <section class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <SectionLabel>{t("detail.sections.transcription")}</SectionLabel>
              <textarea
                rows={4} value={props.event!.transcription_text} placeholder={t("detail.placeholders.transcription")}
                spellcheck={false} class="w-full resize-y rounded-lg border bg-panel px-3 py-2.5 text-[13px] text-main border-subtle focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none"
              />
            </div>
            <div>
              <SectionLabel>{t("detail.sections.notes")}</SectionLabel>
              <textarea
                rows={4} value={props.event!.notes} placeholder={t("detail.placeholders.notes")}
                spellcheck={false} class="w-full resize-y rounded-lg border bg-panel px-3 py-2.5 text-[13px] text-main border-subtle focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none"
              />
            </div>
          </section>
        </Show>
      </div>

      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-t border-subtle bg-panel">
        <Button variant="ghost" size="sm" onClick={props.onReset}>{t("detail.reset")}</Button>
        <div class="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => props.event && props.onSave?.({ ...props.event, people: people() })} disabled={!props.event}>
            {t("detail.save")} <Kbd>Ctrl S</Kbd>
          </Button>
          <Button variant="primary" size="sm" onClick={() => props.event && props.onValidateAndNext?.({ ...props.event, people: people() })} disabled={!props.event}>
            {t("detail.validateAndNext")} <Kbd>↵</Kbd>
          </Button>
        </div>
      </div>
    </div>
  );
};
