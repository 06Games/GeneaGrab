import { createEffect, Show, For } from "solid-js";
import { createForm, reset, insert, remove, SubmitHandler } from "@modular-forms/solid";
import { ACT_TYPE_OPTIONS } from "../../../types/registry";
import { Button, Kbd, SectionLabel } from "../../../ui/primitives";
import { IndexField } from "./IndexField";
import { PersonBlock } from "./PersonBlock";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../../ui/i18n";
import { useRegistryActions } from "../../../contexts/RegistryActionsContext";
import { EventDetail } from "../../../types";

interface DetailZoneProps {
  event: EventDetail | null;
  onRef?: (el: HTMLFormElement) => void;
  onFocusGrid?: () => void;
}

type ActForm = EventDetail;


export const DetailZone = (props: DetailZoneProps) => {
  const { t } = useI18n();
  const actions = useRegistryActions();

  // Initialize Modular Forms
  const [actForm, { Form, Field, FieldArray }] = createForm<ActForm>({
    initialValues: props.event || {}
  });

  // Sync state when user selects a different record in the Grid
  createEffect(() => {
    if (props.event) {
      reset(actForm, { initialValues: props.event });
    } else {
      reset(actForm, { initialValues: {} });
    }
  });

  // Determine what type of submission to make
  let submitAction: 'save' | 'validate' = 'save';

  const handleSubmit: SubmitHandler<ActForm> = (values) => {
    if (submitAction === 'validate') {
      actions.onValidateAndNext?.(values);
    } else {
      actions.onSaveAct?.(values);
    }
  };

  return (
    <Form onSubmit={handleSubmit} ref={props.onRef} class="flex-1 flex flex-col min-w-0 overflow-hidden bg-tinted">
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-b border-subtle bg-panel">
        <div class="flex items-center gap-2 overflow-hidden">
          <span class="text-[13px] font-semibold text-main flex-shrink-0">
            {props.event ? t("detail.headerTitle", { id: props.event.event_id }) : t("detail.noSelection")}
          </span>
          {/* Automatically shows a dirty state marker when modified! */}
          <Show when={actForm.dirty}>
            <span class="text-accent text-[16px] font-bold leading-none select-none" title="Unsaved changes">*</span>
          </Show>
          <Show when={props.event}>
            <span class="text-[12px] text-dim truncate">{props.event!.title || props.event!.date}</span>
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
              {t("detail.emptyPrompt.text")}<br />
              <span class="text-[12px]"><span>{t("detail.emptyPrompt.prefix")}</span> <Kbd>N</Kbd> <span>{t("detail.emptyPrompt.suffix")}</span></span>
            </p>
          </div>
        </Show>

        <Show when={props.event}>
          <section>
            <SectionLabel>{t("detail.sections.details")}</SectionLabel>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-2">
              <Field name="event_type">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.type")} value={field.value} tabIndex={1} options={ACT_TYPE_OPTIONS} />}
              </Field>
              <Field name="title">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.title")} value={field.value} tabIndex={2} placeholder={t("detail.placeholders.title")} />}
              </Field>
              <Field name="act_number">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.actNumber")} value={field.value} tabIndex={3} placeholder={t("detail.placeholders.actNumber")} />}
              </Field>
              <Field name="date">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.dateText")} value={field.value} tabIndex={4} placeholder={t("detail.placeholders.dateText")} />}
              </Field>
              <Field name="date_normalized">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.dateNorm")} value={field.value} tabIndex={5} placeholder={t("detail.placeholders.dateNorm")} />}
              </Field>
              <Field name="page">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.pageFolio")} value={field.value} tabIndex={6} />}
              </Field>
              <Field name="town">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.town")} value={field.value} tabIndex={7} defaultPinned />}
              </Field>
              <Field name="parish">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.parish")} value={field.value} tabIndex={8} defaultPinned />}
              </Field>
              <Field name="hamlet">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.hamlet")} value={field.value} tabIndex={9} defaultPinned />}
              </Field>
              <Field name="image_number">
                {(field, fieldProps) => <IndexField {...fieldProps} label={t("detail.labels.imageNumber")} value={field.value} tabIndex={10} />}
              </Field>
            </div>
          </section>

          <section>
            <SectionLabel>{t("detail.sections.people")}</SectionLabel>
            <div class="flex flex-col gap-2">
              <FieldArray name="people">
                {(fieldArray) => (
                  <For each={fieldArray.items}>
                    {(item, index) => (
                      <PersonBlock
                        index={index()}
                        namePrefix={`people.${index()}.`}
                        form={actForm}
                        Field={Field}
                        tabStart={20 + index() * 20}
                        onRemove={() => remove(actForm, 'people', { at: index() })}
                      />
                    )}
                  </For>
                )}
              </FieldArray>
            </div>

            <button
              type="button"
              onClick={() => insert(actForm, 'people', {
                value: {
                  person_id: crypto.randomUUID(), role: "", first_name: "", last_name: "", sex: "", title: "", age: "",
                  is_deceased: false, occupation: "", origin_place: "", residence_place: "", sequence_number: "", notes: "", relationship_type: "", relationship_to: ""
                }
              })}
              class="mt-2 w-full py-2 rounded-xl border-2 border-dashed border-subtle text-[13px] text-dim flex items-center justify-center gap-1.5 hover:text-muted hover:border-subtle-md hover:bg-panel transition-colors duration-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              <Icon icon="lucide:plus" width="16" height="16" class="block" /> {t("detail.addPerson")}
            </button>
          </section>

          <section class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <SectionLabel>{t("detail.sections.transcription")}</SectionLabel>
              <Field name="transcription_text">
                {(field, fieldProps) => (
                  <textarea
                    {...fieldProps}
                    rows={4} value={field.value ?? ""} placeholder={t("detail.placeholders.transcription")}
                    spellcheck={false} class="w-full resize-y rounded-lg border bg-panel px-3 py-2.5 text-[13px] text-main border-subtle focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none"
                  />
                )}
              </Field>
            </div>
            <div>
              <SectionLabel>{t("detail.sections.notes")}</SectionLabel>
              <Field name="notes">
                {(field, fieldProps) => (
                  <textarea
                    {...fieldProps}
                    rows={4} value={field.value ?? ""} placeholder={t("detail.placeholders.notes")}
                    spellcheck={false} class="w-full resize-y rounded-lg border bg-panel px-3 py-2.5 text-[13px] text-main border-subtle focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none"
                  />
                )}
              </Field>
            </div>
          </section>
        </Show>
      </div>

      <div class="flex-shrink-0 flex items-center justify-between px-4 py-2.5 border-t border-subtle bg-panel">
        <Button variant="ghost" size="sm" onClick={() => reset(actForm)} disabled={!props.event}>{t("detail.reset")}</Button>
        <div class="flex gap-2">
          <Button type="submit" variant="outline" size="sm" onClick={() => submitAction = 'save'} disabled={!props.event}>
            {t("detail.save")} <Kbd>Ctrl S</Kbd>
          </Button>
          <Button type="submit" variant="primary" size="sm" onClick={() => submitAction = 'validate'} disabled={!props.event}>
            {t("detail.validateAndNext")} <Kbd>↵</Kbd>
          </Button>
        </div>
      </div>
    </Form>
  );
};
