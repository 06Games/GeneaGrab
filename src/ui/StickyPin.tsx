import { Icon } from "@iconify-icon/solid";
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
      <Icon icon="lucide:pin" class="w-3.5 h-3.5" aria-hidden="true"></Icon>
    </button>
  );
};
