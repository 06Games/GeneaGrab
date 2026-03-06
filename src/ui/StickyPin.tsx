import { createSignal } from "solid-js";

interface StickyPinProps {
  defaultPinned?: boolean;
  onChange?: (pinned: boolean) => void;
}

export const StickyPin = (props: StickyPinProps) => {
  const [pinned, setPinned] = createSignal(props.defaultPinned ?? false);

  const toggle = () => {
    const next = !pinned();
    setPinned(next);
    props.onChange?.(next);
  };

  return (
    <button
      type="button"
      onClick={toggle}
      title={pinned() ? "Valeur verrouillée (cliquer pour déverrouiller)" : "Verrouiller pour l'acte suivant"}
      class={[
        "flex-shrink-0 w-6 h-6 flex items-center justify-center rounded-md",
        "transition-colors duration-100",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#b8743a]",
        pinned()
          ? "text-[#b8743a] bg-[#fef3e7] border border-[#f0c990]"
          : "text-[#ccc4b8] hover:text-[#a89e93] hover:bg-[#f2ece3] border border-transparent",
      ].join(" ")}
    >
      <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
        {pinned()
          ? <path d="M9.5 1 14 5.5l-1.5 1.5-1-.5-2.8 2.8.5 2.7L7.5 14 6 9.8l-5-1.3 1.5-1.5 2.7.5L8 4.7l-.5-1L9.5 1Z" />
          : <path d="M9.5 1 14 5.5l-1.5 1.5-1-.5-2.8 2.8.5 2.7L7.5 14 6 9.8l-5-1.3 1.5-1.5 2.7.5L8 4.7l-.5-1L9.5 1Zm0 1.4L8.6 3.3l.5 1-3.4 3.4-2.7-.5-.4.4 3.8 1 1 3.8.4-.4-.5-2.7 3.3-3.3 1 .5.9-.9L9.5 2.4Z" />
        }
      </svg>
    </button>
  );
};
