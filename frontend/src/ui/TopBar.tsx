import { JSX, For } from "solid-js";
import { Icon } from "@iconify-icon/solid";

interface TopBarProps {
  breadcrumbs?: (string | JSX.Element)[];
  right?: JSX.Element;
}

export const TopBar = (props: TopBarProps) => {
  return (
    <header data-tauri-drag-region class="flex-shrink-0 flex items-center justify-between px-4 h-11 bg-panel border-b border-subtle shadow-sm z-10 select-none">
      <div class="flex items-center gap-2 min-w-0 overflow-hidden">
        <a href="/" class="text-[15px] font-bold text-accent flex-shrink-0 hover:text-accent-hover transition-colors">
          GeneaGrab
        </a>
        <For each={props.breadcrumbs}>
          {(crumb) => (
            <>
              <Icon icon="lucide:chevron-right" class="w-3.5 h-3.5 text-subtle-md flex-shrink-0 pointer-events-none" />
              <span class="text-[13px] text-dim flex-shrink-0 truncate last:text-main last:font-medium [&>a]:hover:text-main [&>a]:transition-colors">
                {crumb}
              </span>
            </>
          )}
        </For>
      </div>
      <div class="flex items-center gap-2 flex-shrink-0 pointer-events-auto">{props.right}</div>
    </header>
  );
};
