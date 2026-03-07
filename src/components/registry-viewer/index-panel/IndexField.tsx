import { Show, createUniqueId } from "solid-js";
import { StickyPin } from "../../../ui/StickyPin";

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

/**
 * IndexField — a labelled input row with optional StickyPin, datalist, or checkbox.
 */
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
          "flex-shrink-0 text-[12px] text-[#a89e93] text-right truncate select-none cursor-pointer"
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
              "w-full h-full px-3 rounded-lg border text-[13px] text-[#2c2820]",
              "bg-[#faf7f3] border-[#e0d8cc] placeholder:text-[#ccc4b8]",
              "focus:border-[#b8743a] focus:ring-2 focus:ring-[#b8743a]/15 focus:outline-none",
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
                  "bg-[#faf7f3] border-[#e0d8cc]",
                  "focus:border-[#b8743a] focus:ring-2 focus:ring-[#b8743a]/15 focus:outline-none",
                  "checked:bg-[#b8743a] checked:border-[#b8743a]",
                  "transition-all duration-100"
                ].join(" ")}
              />
              <Show when={!!props.value}>
                <svg
                  class="absolute inset-0 m-auto w-4 h-4 text-white pointer-events-none"
                  viewBox="0 0 16 16"
                  fill="currentColor"
                >
                  <path d="M12.207 4.793a1 1 0 010 1.414l-5 5a1 1 0 01-1.414 0l-2-2a1 1 0 011.414-1.414L6.5 9.086l4.293-4.293a1 1 0 011.414 0z" />
                </svg>
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
