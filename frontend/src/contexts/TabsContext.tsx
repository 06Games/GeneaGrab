import { createContext, useContext, JSX, createSignal } from "solid-js";
import { createStore } from "solid-js/store";

export type TabType = "home" | "registry";

export interface Tab {
  id: string;
  type: TabType;
  title: string;
  closable: boolean;
  registryId?: number;
  imageId?: number;
}

interface TabsContextValue {
  tabs: Tab[];
  activeTabId: () => string;
  openTab: (tab: Omit<Tab, "id" | "title" | "closable"> & { id?: string }, activate?: boolean) => void;
  closeTab: (id: string) => void;
  setActiveTab: (id: string) => void;
  updateTab: (id: string, updates: Partial<Tab>) => void;
}

const TabsContext = createContext<TabsContextValue>();

export function TabsProvider(props: { children: JSX.Element }) {
  const [tabs, setTabs] = createStore<Tab[]>([{ id: "home", type: "home", title: "Home", closable: false }]);
  const [activeTabId, setActiveTabId] = createSignal("home");

  const openTab = (newTab: Omit<Tab, "id" | "title" | "closable"> & { id?: string }, activate = true) => {
    const id = newTab.id ?? (newTab.type === "home" ? "home" : `${newTab.type}-${newTab.registryId}`);

    const existingTab = tabs.find((t) => t.id === id);
    if (existingTab) {
      if (newTab.imageId !== undefined && existingTab.imageId !== newTab.imageId) {
        setTabs((t) => t.id === id, { imageId: newTab.imageId });
      }
      if (activate) {
        setActiveTabId(id);
      }
      return;
    }

    setTabs([...tabs, { ...newTab, id, title: id, imageId: newTab.imageId, closable: true }]);
    if (activate) {
      setActiveTabId(id);
    }
  };
  const closeTab = (id: string) => {
    if (id === "home") return; // Prevent closing the home tab

    const index = tabs.findIndex((t) => t.id === id);
    if (index === -1) return;

    setTabs((t) => t.filter((tab) => tab.id !== id));

    // If we closed the active tab, fallback to the previous one or home
    if (activeTabId() === id) {
      const nextTab = tabs[index - 1] || tabs[0];
      setActiveTabId(nextTab.id);
    }
  };

  const updateTab = (id: string, updates: Partial<Tab>) => {
    setTabs((tab) => tab.id === id, updates);
  };

  return (
    <TabsContext.Provider value={{ tabs, activeTabId, openTab, closeTab, setActiveTab: setActiveTabId, updateTab }}>{props.children}</TabsContext.Provider>
  );
}

export function useTabs() {
  const context = useContext(TabsContext);
  if (!context) throw new Error("useTabs must be used within a TabsProvider");
  return context;
}

interface TabInstanceContextValue {
  tabId: string;
  updateThisTab: (updates: Partial<Tab>) => void;
}

const TabInstanceContext = createContext<TabInstanceContextValue>();

// A wrapper provider that binds the update function to a specific ID
export function TabInstanceProvider(props: { tabId: string; children: JSX.Element }) {
  const { updateTab } = useTabs();

  const updateThisTab = (updates: Partial<Tab>) => {
    updateTab(props.tabId, updates);
  };

  return <TabInstanceContext.Provider value={{ tabId: props.tabId, updateThisTab }}>{props.children}</TabInstanceContext.Provider>;
}

export function useCurrentTab() {
  const context = useContext(TabInstanceContext);
  if (!context) throw new Error("useCurrentTab must be used inside a TabInstanceProvider");
  return context;
}
