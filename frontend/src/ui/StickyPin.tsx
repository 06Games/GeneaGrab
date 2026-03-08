import { Icon } from "@iconify-icon/solid";
import { createSignal } from "solid-js";
import { useI18n } from "./i18n";

interface StickyPinProps {
  defaultPinned?: boolean;
  onChange?: (pinned: boolean) => void;
}

export const StickyPin = (props: StickyPinProps) => {
  const [pinned, setPinned] = createSignal(props.defaultPinned ?? false);
  const { t } = useI18n();

  const toggle = () => {
    const next = !pinned();
    setPinned(next);
    props.onChange?.(next);
  };

  return (
    <button
      type="button"
      onClick={toggle}
      title={pinned() ? t("stickyPin.locked") : t("stickyPin.lockForNext")}
      class={[
        "flex-shrink-0 w-6 h-6 flex items-center justify-center rounded-md",
        "transition-colors duration-100",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
        pinned()
          ? "text-accent bg-accent-bg border border-accent-border"
          : "text-subtle-md hover:text-dim hover:bg-hover border border-transparent",
      ].join(" ")}
    >
      <Icon icon="lucide:pin" class="w-3.5 h-3.5" aria-hidden="true"></Icon>
    </button>
  );
};
