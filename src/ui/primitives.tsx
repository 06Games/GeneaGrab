import { JSX } from "solid-js";

// ─────────────────────────────────────────────────────────────────────────────
// DESIGN TOKENS — warm archival light theme
//
//  bg-app      #f7f3ee   warm cream shell
//  bg-panel    #ffffff   panel surfaces
//  bg-tinted   #faf7f3   slightly tinted surface
//  bg-hover    #f2ece3   hover state
//  bg-active   #efe8dc   active / selected
//  border      #e0d8cc   dividers
//  border-md   #ccc4b8   medium emphasis border
//  text        #2c2820   near-black warm
//  text-2      #6b6358   secondary text
//  text-3      #a89e93   tertiary / placeholder
//  accent      #b8743a   warm terracotta amber
//  accent-bg   #fef3e7   accent tint
//  accent-text #7a4a1e   dark accent for text on light
//  success     #3a8c5c
//  danger      #c0392b
// ─────────────────────────────────────────────────────────────────────────────

// ─── Divider ─────────────────────────────────────────────────────────────────

interface DividerProps { vertical?: boolean; class?: string }

export const Divider = (props: DividerProps) => (
  <div class={[
    "bg-[#e0d8cc] flex-shrink-0",
    props.vertical ? "w-px self-stretch" : "h-px w-full",
    props.class,
  ].filter(Boolean).join(" ")} />
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
      "bg-[#ede8e1] hover:bg-[#b8743a]/25 active:bg-[#b8743a]/45",
      "transition-colors duration-150",
      props.vertical
        ? "w-[6px] h-full cursor-col-resize"
        : "h-[6px] w-full cursor-row-resize",
      props.class,
    ].filter(Boolean).join(" ")}
  >
    <div class={[
      "flex gap-[3px] opacity-0 group-hover:opacity-70 transition-opacity",
      props.vertical ? "flex-col" : "flex-row",
    ].join(" ")}>
      {[0, 1, 2].map(() => (
        <div class="w-1 h-1 rounded-full bg-[#b8743a]" />
      ))}
    </div>
  </div>
);

// ─── Badge ────────────────────────────────────────────────────────────────────

interface BadgeProps { class?: string; children: JSX.Element }

export const Badge = (props: BadgeProps) => (
  <span class={[
    "inline-flex items-center px-2 py-0.5 rounded-full",
    "text-[11px] font-medium",
    "border border-[#e0d8cc] bg-[#faf7f3] text-[#7a4a1e]",
    props.class,
  ].filter(Boolean).join(" ")}>
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
    "focus-visible:ring-2 focus-visible:ring-[#b8743a] focus-visible:ring-offset-1",
    "disabled:opacity-40 disabled:cursor-not-allowed",
    size === "sm"  ? "px-2.5 py-1 text-[13px]" : "px-3.5 py-1.5 text-[14px]",
  ];

  const variants = {
    ghost:   "text-[#6b6358] hover:text-[#2c2820] hover:bg-[#f2ece3]",
    outline: "text-[#2c2820] border border-[#e0d8cc] bg-white hover:bg-[#f7f3ee] hover:border-[#ccc4b8]",
    primary: "text-white bg-[#b8743a] hover:bg-[#a06530] shadow-sm",
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
  class?: string;
  children: JSX.Element;
}

export const IconButton = (props: IconButtonProps) => (
  <button
    type="button"
    title={props.title}
    onClick={props.onClick}
    class={[
      "flex items-center justify-center w-8 h-8 rounded-md",
      "transition-colors duration-100",
      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#b8743a]",
      props.active
        ? "text-[#b8743a] bg-[#fef3e7]"
        : "text-[#6b6358] hover:text-[#2c2820] hover:bg-[#f2ece3]",
      props.class,
    ].filter(Boolean).join(" ")}
  >
    {props.children}
  </button>
);

// ─── Kbd ──────────────────────────────────────────────────────────────────────

interface KbdProps { class?: string; children: JSX.Element }

export const Kbd = (props: KbdProps) => (
  <kbd class={[
    "px-1.5 py-0.5 rounded text-[10px] font-mono",
    "bg-[#f2ece3] border border-[#ccc4b8] text-[#6b6358]",
    "shadow-[0_1px_0_#ccc4b8]",
    props.class,
  ].filter(Boolean).join(" ")}>
    {props.children}
  </kbd>
);

// ─── MetaRow ──────────────────────────────────────────────────────────────────

interface MetaRowProps { label: string; value?: string | null }

export const MetaRow = (props: MetaRowProps) => (
  <div class="grid grid-cols-[8rem_1fr] gap-x-3 items-baseline py-1">
    <span class="text-[12px] text-[#a89e93] truncate capitalize">{props.label}</span>
    {props.value
      ? <span class="text-[13px] text-[#2c2820] truncate" title={props.value}>{props.value}</span>
      : <span class="text-[13px] text-[#ccc4b8] italic">—</span>
    }
  </div>
);

// ─── SectionLabel ─────────────────────────────────────────────────────────────

interface SectionLabelProps { children: JSX.Element; class?: string }

export const SectionLabel = (props: SectionLabelProps) => (
  <div class={["flex items-center gap-2 mb-2", props.class].filter(Boolean).join(" ")}>
    <span class="text-[11px] font-semibold uppercase tracking-wider text-[#a89e93]">
      {props.children}
    </span>
    <div class="flex-1 h-px bg-[#e0d8cc]" />
  </div>
);
