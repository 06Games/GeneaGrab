import { Show } from "solid-js";
import { StickyPin } from "../../../ui/StickyPin";

interface IndexFieldProps {
  label: string;
  value?: string;
  placeholder?: string;
  tabIndex?: number;
  options?: string[];
  defaultPinned?: boolean;
  onPinChange?: (pinned: boolean) => void;
  onInput?: (value: string) => void;
  class?: string;
}

/**
 * IndexField — a labelled input row with optional StickyPin and datalist.
 * Uses native <datalist> for zero-dependency combobox autocomplete.
 */
export const IndexField = (props: IndexFieldProps) => {
  const listId = `dl-${props.label.toLowerCase().replace(/[\s']/g, "-")}`;

  return (
    <div class={["flex items-center gap-2", props.class].filter(Boolean).join(" ")}>
      <label
        for={`field-${listId}`}
        class="w-28 flex-shrink-0 text-[12px] text-[#a89e93] text-right truncate select-none"
      >
        {props.label}
      </label>

      <div class="flex-1 min-w-0 relative">
        <input
          id={`field-${listId}`}
          type="text"
          value={props.value ?? ""}
          placeholder={props.placeholder ?? ""}
          tabIndex={props.tabIndex}
          list={props.options ? listId : undefined}
          onInput={(e) => props.onInput?.(e.currentTarget.value)}
          autocomplete="off"
          spellcheck={false}
          class={[
            "w-full h-8 px-3 rounded-lg border text-[13px] text-[#2c2820]",
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
      </div>

      <StickyPin defaultPinned={props.defaultPinned} onChange={props.onPinChange} />
    </div>
  );
};
