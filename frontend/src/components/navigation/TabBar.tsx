import { For } from "solid-js";
import { useTabs } from "../../contexts/TabsContext";
import { Icon } from "@iconify-icon/solid";

export const TabBar = () => {
  const { tabs, activeTabId, setActiveTab, closeTab } = useTabs();

  return (
    <div class="flex items-center w-full h-10 bg-panel border-b border-subtle overflow-x-auto select-none">
      <For each={tabs}>
        {(tab) => {
          const isActive = () => activeTabId() === tab.id;

          return (
            <div
              class={[
                "flex items-center gap-2 h-full px-4 border-r border-subtle cursor-pointer transition-colors",
                isActive()
                  ? "bg-app text-accent font-medium border-t-2 border-t-accent"
                  : "bg-panel text-dim hover:bg-tinted hover:text-main border-t-2 border-t-transparent",
              ].join(" ")}
              onClick={() => setActiveTab(tab.id)}
              onAuxClick={(e) => {
                if (e.button === 1 && tab.closable) {
                  e.preventDefault();
                  closeTab(tab.id);
                }
              }}
            >
              <span class="text-[13px] truncate max-w-[300px]" title={tab.title}>
                {tab.title}
              </span>
              {tab.closable && (
                <button
                  class="p-0.5 rounded hover:bg-subtle/50 text-subtle-md hover:text-danger transition-colors"
                  onClick={(e) => {
                    e.stopPropagation();
                    closeTab(tab.id);
                  }}
                >
                  <Icon icon="lucide:x" class="w-3.5 h-3.5 block" />
                </button>
              )}
            </div>
          );
        }}
      </For>
    </div>
  );
};
