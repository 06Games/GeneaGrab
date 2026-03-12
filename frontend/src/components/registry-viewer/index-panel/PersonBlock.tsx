import { createSignal, Show } from "solid-js";
import { getValue } from "@modular-forms/solid";
import { IconButton } from "../../../ui/primitives";
import { IndexField } from "./IndexField";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../../ui/i18n";
import { ROLE_SUGGESTIONS, PROFESSION_OPTIONS, RELATION_SUGGESTIONS } from "../../../types";

interface PersonBlockProps {
  index: number;
  namePrefix: string;
  form: any;
  Field: any;
  tabStart: number;
  onRemove: () => void;
}

export const PersonBlock = (props: PersonBlockProps) => {
  const [collapsed, setCollapsed] = createSignal(false);
  const { t } = useI18n();

  const role = () => getValue(props.form, `${props.namePrefix}role`) as string | undefined;
  const title = () => getValue(props.form, `${props.namePrefix}title`) as string | undefined;
  const firstName = () => getValue(props.form, `${props.namePrefix}first_name`) as string | undefined;
  const lastName = () => getValue(props.form, `${props.namePrefix}last_name`) as string | undefined;
  const isDeceased = () => getValue(props.form, `${props.namePrefix}is_deceased`) as boolean | undefined;

  const displayName = () => {
    const parts = [title(), firstName(), lastName()].filter(Boolean);
    return parts.length > 0 ? parts.join(" ") : undefined;
  };

  const { Field } = props;

  return (
    <div class="border border-subtle rounded-xl overflow-hidden bg-panel shadow-sm">
      {/* Header */}
      <div class="flex items-center px-3 py-2 gap-2 bg-tinted border-b border-subtle">
        <span class="text-subtle-md cursor-grab select-none flex-shrink-0">
          <Icon icon="lucide:grip-vertical" class="block"></Icon>
        </span>

        <button
          type="button"
          onClick={() => setCollapsed(v => !v)}
          class="flex-1 flex items-center gap-2 text-left focus-visible:outline-none min-w-0"
        >
          <span class="text-[12px] font-semibold px-2 py-0.5 rounded-full bg-accent-bg text-accent-text border border-accent-border flex-shrink-0">
            {role() || <span class="italic text-dim">{t("person.noRole")}</span>}
          </span>
          <Show when={displayName()}>
            <span class="text-[13px] text-muted truncate">
              {isDeceased() && <span class="text-xs mr-1 text-danger font-bold">†</span>}
              {displayName()}
            </span>
          </Show>
        </button>

        <IconButton onClick={() => setCollapsed(v => !v)}>
          <span class={["transition-transform duration-150 inline-flex items-center justify-center origin-center", collapsed() ? "-rotate-90" : ""].join(" ")}>
            <Icon icon="lucide:chevron-down" class="block" />
          </span>
        </IconButton>
        <IconButton title={t("person.remove")} onClick={props.onRemove} class="text-danger opacity-50 hover:opacity-100">
          <Icon icon="lucide:trash-2" class="block"></Icon>
        </IconButton>
      </div>

      {/* Fields */}
      <Show when={!collapsed()}>
        <div class="px-3 pt-3 pb-3 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-4 gap-y-2">

          <Field name={`${props.namePrefix}role`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.role")} tabIndex={props.tabStart} options={ROLE_SUGGESTIONS} class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}first_name`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.firstNames")} tabIndex={props.tabStart + 1} class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}last_name`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.lastName")} tabIndex={props.tabStart + 2} class="lg:col-span-1" />}
          </Field>

          <Field name={`${props.namePrefix}title`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.title")} tabIndex={props.tabStart + 3} placeholder={t("person.title") + "..."} class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}sex`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.sex")} tabIndex={props.tabStart + 4} options={["M", "F", t("person.noRole")]} class="lg:col-span-1" />}
          </Field>

          <div class="flex gap-2 lg:col-span-1">
            <Field name={`${props.namePrefix}age`}>
              {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.age")} tabIndex={props.tabStart + 5} class="flex-1 min-w-0" />}
            </Field>
            <Field name={`${props.namePrefix}is_deceased`} type="boolean">
              {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.deceased")} type="checkbox" tabIndex={props.tabStart + 6} class="w-[85px] flex-shrink-0" labelWidth="w-auto" />}
            </Field>
          </div>

          <Field name={`${props.namePrefix}occupation`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.profession")} tabIndex={props.tabStart + 7} options={PROFESSION_OPTIONS} defaultPinned class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}origin_place`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.origin")} tabIndex={props.tabStart + 8} defaultPinned class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}residence_place`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.residence")} tabIndex={props.tabStart + 9} defaultPinned class="lg:col-span-1" />}
          </Field>

          <Field name={`${props.namePrefix}relationship_type`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.relationship")} tabIndex={props.tabStart + 10} options={RELATION_SUGGESTIONS} placeholder={t("person.relationship") + "..."} class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}relationship_to`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.relationshipTo")} tabIndex={props.tabStart + 11} placeholder={t("person.relationshipTo") + "..."} class="lg:col-span-1" />}
          </Field>
          <Field name={`${props.namePrefix}sequence_number`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.sequenceNumber")} tabIndex={props.tabStart + 12} class="lg:col-span-1" />}
          </Field>

          <Field name={`${props.namePrefix}notes`}>
            {(field: any, fieldProps: any) => <IndexField {...fieldProps} value={field.value} label={t("person.notesIndiv")} tabIndex={props.tabStart + 13} class="md:col-span-2 lg:col-span-3" />}
          </Field>
        </div>
      </Show>
    </div>
  );
};
