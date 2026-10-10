import { For } from "solid-js";
import { useTabs } from "../../contexts/TabsContext";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";

export const TabBar = () => {
  const { tabs, activeTabId, setActiveTab, closeTab, setActionsContainer } = useTabs();
  const { t } = useI18n();

  return (
    <header data-tauri-drag-region class="flex items-center justify-between w-full h-10 bg-panel border-b border-subtle select-none flex-shrink-0 z-20">
      <div class="flex items-center h-full overflow-x-auto min-w-0 flex-1 scrollbar-none">
        <For each={tabs}>
          {(tab) => {
            const isActive = () => activeTabId() === tab.id;

            return (
              <div
                class={[
                  "flex items-center gap-2 h-full px-4 border-r border-subtle cursor-pointer transition-colors flex-shrink-0",
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
                {tab.type === "home" && <Icon icon="lucide:home" class="w-3.5 h-3.5 flex-shrink-0" />}
                <span class="text-[13px] truncate max-w-[300px]" title={tab.type === "home" ? t("home.title") : tab.title}>
                  {tab.type === "home" ? t("home.title") : tab.title}
                </span>
                {tab.closable && (
                  <button
                    class="p-0.5 rounded hover:bg-subtle/50 text-subtle-md hover:text-danger transition-colors cursor-pointer"
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

      <div ref={setActionsContainer} class="flex items-center gap-2 px-3 h-full flex-shrink-0 pointer-events-auto" />
    </header>
  );
};
