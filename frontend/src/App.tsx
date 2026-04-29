import { For } from "solid-js";
import { TabInstanceProvider, TabsProvider, useTabs } from "./contexts/TabsContext";
import HomePage from "./pages/HomePage";
import ViewerPage from "./pages/ViewerPage";
import { TabBar } from "./components/navigation/TabBar";

const AppContent = () => {
  const { tabs, activeTabId } = useTabs();

  return (
    <div class="w-screen h-screen flex flex-col bg-app">
      <TabBar />
      <div class="flex-1 relative overflow-hidden">
        <For each={tabs}>
          {(tab) => (
            <div class="absolute inset-0 flex flex-col" style={{ display: activeTabId() === tab.id ? "flex" : "none" }}>
              {/* Inject the scoped context right here! */}
              <TabInstanceProvider tabId={tab.id}>
                {tab.type === "home" && <HomePage />}
                {tab.type === "registry" && <ViewerPage registryId={tab.registryId!} initialImageId={tab.imageId} />}
              </TabInstanceProvider>
            </div>
          )}
        </For>
      </div>
    </div>
  );
};

const App = () => {
  return (
    <TabsProvider>
      <AppContent />
    </TabsProvider>
  );
};

export default App;
