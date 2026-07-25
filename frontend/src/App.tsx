import { For, createSignal, onMount, onCleanup } from "solid-js";
import { TabInstanceProvider, TabsProvider, useTabs } from "./contexts/TabsContext";
import HomePage from "./pages/HomePage";
import ViewerPage from "./pages/ViewerPage";
import { TabBar } from "./components/navigation/TabBar";
import { listen } from "@tauri-apps/api/event";
import { invoke } from "@tauri-apps/api/core";

const AppContent = () => {
  const { tabs, activeTabId } = useTabs();
  const [captchaImg, setCaptchaImg] = createSignal<string | null>(null);
  const [captchaCode, setCaptchaCode] = createSignal("");

  let unsubscribe: (() => void) | undefined;

  onMount(async () => {
    unsubscribe = await listen<string>("show-captcha", (event) => {
      setCaptchaImg(event.payload);
      setCaptchaCode("");
    });
  });

  onCleanup(() => {
    if (unsubscribe) unsubscribe();
  });

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    const code = captchaCode().trim();
    if (!code) return;
    setCaptchaImg(null);
    await invoke("submit_captcha_code", { code });
  };

  return (
    <div class="w-screen h-screen flex flex-col bg-app overflow-hidden">
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

      {captchaImg() && (
        <div class="fixed inset-0 bg-black/50 backdrop-blur-[2px] z-50 flex items-center justify-center p-4">
          <div class="bg-[var(--panel)] border border-[var(--subtle)] rounded-xl p-6 shadow-xl max-w-sm w-full flex flex-col gap-4">
            <div class="flex flex-col gap-1">
              <h3 class="text-lg font-bold text-[var(--main)]">Enter Verification Code</h3>
              <p class="text-xs text-[var(--muted)]">Please type the characters shown below to bypass departmental archives protection.</p>
            </div>
            
            <div class="bg-[var(--tinted)] border border-[var(--subtle)] rounded-lg p-4 flex items-center justify-center select-none">
              <img src={`data:image/png;base64,${captchaImg()}`} class="h-12 object-contain" alt="Captcha" />
            </div>

            <form onSubmit={handleSubmit} class="flex flex-col gap-3">
              <input
                type="text"
                value={captchaCode()}
                onInput={(e) => setCaptchaCode(e.currentTarget.value)}
                placeholder="Captcha code"
                class="w-full text-center uppercase tracking-widest font-mono text-xl py-2 px-3 bg-[var(--tinted)] border border-[var(--subtle)] rounded-lg text-[var(--main)] focus:outline-none focus:border-[var(--accent)]"
                autofocus
              />
              <button
                type="submit"
                class="w-full py-2 bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white font-semibold rounded-lg transition-colors cursor-pointer"
              >
                Submit
              </button>
            </form>
          </div>
        </div>
      )}
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
