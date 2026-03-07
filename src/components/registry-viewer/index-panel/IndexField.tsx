import { Show, createUniqueId } from "solid-js";
import { StickyPin } from "../../../ui/StickyPin";
import { Icon } from "@iconify-icon/solid";

interface IndexFieldProps {
  label: string;
  value?: string | boolean;
  type?: "text" | "checkbox";
  placeholder?: string;
  tabIndex?: number;
  options?: string[];
  defaultPinned?: boolean;
  labelWidth?: string;
  onPinChange?: (pinned: boolean) => void;
  onInput?: (value: any) => void;
  class?: string;
}

// IndexField — labelled input with optional pin, datalist or checkbox
export const IndexField = (props: IndexFieldProps) => {
  const uniqueId = createUniqueId();
  const listId = `dl-${uniqueId}`;
  const fieldId = `field-${uniqueId}`;

  const isCheckbox = props.type === "checkbox";

  return (
    <div class={["flex items-center gap-2", props.class].filter(Boolean).join(" ")}>
      <label
        for={fieldId}
        title={props.label}
        class={[
          props.labelWidth || "w-24",
          "flex-shrink-0 text-[12px] text-dim text-right truncate select-none cursor-pointer"
        ].join(" ")}
      >
        {props.label}
      </label>

      <div class="flex-1 min-w-0 relative flex items-center h-8">
        <Show when={!isCheckbox}>
          <input
            id={fieldId}
            type="text"
            value={(props.value as string) ?? ""}
            placeholder={props.placeholder ?? ""}
            tabIndex={props.tabIndex}
            list={props.options ? listId : undefined}
            onInput={(e) => props.onInput?.(e.currentTarget.value)}
            autocomplete="off"
            spellcheck={false}
            class={[
              "w-full h-full px-3 rounded-lg border text-[13px] text-main",
              "bg-tinted border-subtle placeholder:text-subtle-md",
              "focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none",
              "transition-all duration-100",
            ].join(" ")}
          />
          <Show when={props.options}>
            <datalist id={listId}>
              {props.options!.map(opt => <option value={opt} />)}
            </datalist>
          </Show>
        </Show>

        <Show when={isCheckbox}>
          <div class="flex items-center w-full h-full">

            <div class="relative w-5 h-5">
              <input
                id={fieldId}
                type="checkbox"
                checked={!!props.value}
                tabIndex={props.tabIndex}
                onChange={(e) => props.onInput?.(e.currentTarget.checked)}
                class={[
                  "appearance-none cursor-pointer m-0 w-full h-full rounded border",
                  "bg-tinted border-subtle",
                  "focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none",
                  "checked:bg-accent checked:border-accent",
                  "transition-all duration-100"
                ].join(" ")}
              />
              <Show when={!!props.value}>
                <Icon icon="lucide:check" class="absolute inset-0 m-auto w-4 h-4 text-white pointer-events-none"></Icon>
              </Show>
            </div>
          </div>
        </Show>
      </div>

      <Show when={!isCheckbox}>
        <StickyPin defaultPinned={props.defaultPinned} onChange={props.onPinChange} />
      </Show>
    </div>
  );
};
