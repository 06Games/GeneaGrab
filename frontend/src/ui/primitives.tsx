import { JSX } from "solid-js";

// ─── Divider ─────────────────────────────────────────────────────────────────

interface DividerProps {
  vertical?: boolean;
  class?: string;
}

export const Divider = (props: DividerProps) => (
  <div class={["bg-subtle flex-shrink-0", props.vertical ? "w-px self-stretch" : "h-px w-full", props.class].filter(Boolean).join(" ")} />
);

// ─── ResizeHandle ─────────────────────────────────────────────────────────────

interface ResizeHandleProps {
  vertical?: boolean;
  class?: string;
  onPointerDown?: JSX.EventHandler<HTMLDivElement, PointerEvent>;
}

export const ResizeHandle = (props: ResizeHandleProps) => (
  <div
    onPointerDown={props.onPointerDown}
    class={[
      "group relative flex items-center justify-center select-none z-10 flex-shrink-0",
      "bg-active hover:bg-accent/25 active:bg-accent/45",
      "transition-colors duration-150",
      props.vertical ? "w-[6px] h-full cursor-col-resize" : "h-[6px] w-full cursor-row-resize",
      props.class,
    ]
      .filter(Boolean)
      .join(" ")}
  >
    <div class={["flex gap-[3px] opacity-0 group-hover:opacity-70 transition-opacity", props.vertical ? "flex-col" : "flex-row"].join(" ")}>
      {[0, 1, 2].map(() => (
        <div class="w-1 h-1 rounded-full bg-accent" />
      ))}
    </div>
  </div>
);

// ─── Badge ────────────────────────────────────────────────────────────────────

interface BadgeProps {
  class?: string;
  children: JSX.Element;
}

export const Badge = (props: BadgeProps) => (
  <span
    class={["inline-flex items-center px-2 py-0.5 rounded-full", "text-[11px] font-medium", "border border-subtle bg-tinted text-accent-text", props.class]
      .filter(Boolean)
      .join(" ")}
  >
    {props.children}
  </span>
);

// ─── Button ───────────────────────────────────────────────────────────────────

interface ButtonProps {
  title?: string;
  onClick?: JSX.EventHandler<HTMLButtonElement, MouseEvent>;
  variant?: "ghost" | "outline" | "primary";
  size?: "sm" | "md";
  class?: string;
  children: JSX.Element;
  type?: "button" | "submit" | "reset";
  disabled?: boolean;
}

export const Button = (props: ButtonProps) => {
  const size = props.size ?? "sm";
  const variant = props.variant ?? "ghost";

  const base = [
    "inline-flex items-center justify-center gap-1.5 rounded-md font-medium",
    "transition-colors duration-100 focus-visible:outline-none",
    "focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-1",
    "disabled:opacity-40 disabled:cursor-not-allowed",
    size === "sm" ? "px-2.5 py-1 text-[13px]" : "px-3.5 py-1.5 text-[14px]",
  ];

  const variants = {
    ghost: "text-muted enabled:hover:text-main enabled:hover:bg-hover",
    outline: "text-main border border-subtle bg-panel enabled:hover:bg-app enabled:hover:border-subtle-md",
    primary: "text-white bg-accent enabled:hover:bg-accent-hover shadow-sm",
  };

  return (
    <button
      type={props.type ?? "button"}
      title={props.title}
      onClick={props.onClick}
      disabled={props.disabled}
      class={[...base, variants[variant], props.class].filter(Boolean).join(" ")}
    >
      {props.children}
    </button>
  );
};

// ─── IconButton ───────────────────────────────────────────────────────────────

interface IconButtonProps {
  title?: string;
  onClick?: JSX.EventHandler<HTMLButtonElement, MouseEvent>;
  active?: boolean;
  disabled?: boolean;
  class?: string;
  children: JSX.Element;
}

export const IconButton = (props: IconButtonProps) => (
  <button
    type="button"
    title={props.title}
    onClick={props.onClick}
    disabled={props.disabled}
    class={[
      "flex items-center justify-center w-8 h-8 rounded-md",
      "transition-colors duration-100",
      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
      "disabled:opacity-40 disabled:cursor-not-allowed",
      props.active ? "text-accent bg-accent-bg" : "text-muted enabled:hover:text-main enabled:hover:bg-hover",
      props.class,
    ]
      .filter(Boolean)
      .join(" ")}
  >
    {props.children}
  </button>
);

// ─── Kbd ──────────────────────────────────────────────────────────────────────

interface KbdProps {
  class?: string;
  children: JSX.Element;
}

export const Kbd = (props: KbdProps) => (
  <kbd
    class={["px-1.5 py-0.5 rounded text-[10px] font-mono", "bg-hover border border-subtle-md text-muted", "shadow-[0_1px_0_var(--subtle-md)]", props.class]
      .filter(Boolean)
      .join(" ")}
  >
    {props.children}
  </kbd>
);

// ─── MetaRow ──────────────────────────────────────────────────────────────────

interface MetaRowProps {
  label: string;
  value?: string | null;
}

export const MetaRow = (props: MetaRowProps) => (
  <div class="grid grid-cols-[8rem_1fr] gap-x-3 items-baseline py-1">
    <span class="text-[12px] text-dim truncate capitalize">{props.label}</span>
    {props.value ? (
      <span class="text-[13px] text-main truncate" title={props.value}>
        {props.value}
      </span>
    ) : (
      <span class="text-[13px] text-subtle-md italic">—</span>
    )}
  </div>
);

// ─── SectionLabel ─────────────────────────────────────────────────────────────

interface SectionLabelProps {
  children: JSX.Element;
  class?: string;
}

export const SectionLabel = (props: SectionLabelProps) => (
  <div class={["flex items-center gap-2 mb-2", props.class].filter(Boolean).join(" ")}>
    <span class="text-[11px] font-semibold uppercase tracking-wider text-dim">{props.children}</span>
    <div class="flex-1 h-px bg-subtle" />
  </div>
);
