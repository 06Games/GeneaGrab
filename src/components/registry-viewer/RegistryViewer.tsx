import { createSignal, createEffect, onCleanup, onMount } from "solid-js";
import type { ActDetail, ActRow, ActType, ImageMeta, RegistryMeta } from "../../types/registry";
import { Kbd } from "../../ui/primitives";
import { MainViewer } from "./MainViewer";
import { ThumbnailBar } from "./ThumbnailBar";
import { InfoNotesPanel } from "./InfoNotesPanel";
import { IndexPanel } from "./index-panel/IndexPanel";

// ─── Mock data ────────────────────────────────────────────────────────────────

const MOCK_REGISTRY: RegistryMeta = {
  archive: "Archives Dép. du Var",
  fond:    "État Civil",
  cote:    "5 Mi 1/342",
  commune: "Brignoles",
  period:  "1792 – 1832",
  type:    "Naissances",
  source:  "AD83",
};

const MOCK_IMAGE: ImageMeta = {
  folio:        "12r",
  dateRange:    "3 Frimaire An II",
  actTypes:     new Map<ActType, number>([
    ["Naissance", 2],
    ["Mariage", 1],
    ["Décès", 1],
  ]),
};

const MOCK_ROWS: ActRow[] = [
  { id: 1, date: "03 Frim. II", type: "Naissance", subject: "MARTIN, Jean-Baptiste" },
  { id: 2, date: "03 Frim. II", type: "Naissance", subject: "DUPONT, Marie" },
  { id: 3, date: "05 Frim. II", type: "Mariage",   subject: "ARNAUD, Pierre ∞ BLANC" },
  { id: 4, date: "05 Frim. II", type: "Décès",     subject: "BOYER, Antoinette" },
  { id: 5, date: "07 Frim. II", type: "Naissance", subject: "ISNARD, Louis" },
  { id: 6, date: "12 Frim. II", type: "Naissance", subject: "FABRE, Thérèse" },
  { id: 7, date: "14 Frim. II", type: "Mariage",   subject: "ROUX, Antoine ∞ AUBERT" },
  { id: 8, date: "16 Frim. II", type: "Décès",     subject: "PASCAL, Jean" },
];

const MOCK_ACT: ActDetail = {
  id: 3, date: "05 Frimaire An II", type: "Mariage",
  folioRef: "12r", actNumber: "47",
  people: [
    {
      id: "p1", relation: "Déclarant",
      nom: "ARNAUD", prenoms: "Pierre",
      ageOrBorn: "27 ans", profession: "Laboureur", domicile: "Brignoles",
    },
    {
      id: "p2", relation: "Épouse",
      nom: "BLANC", prenoms: "Marie",
      ageOrBorn: "22 ans", profession: "", domicile: "Brignoles",
    },
  ],
  remarks: "",
};

// ─── Icons ────────────────────────────────────────────────────────────────────

const IndexIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M1 3h14v2H1V3Zm0 4h14v2H1V7Zm0 4h8v2H1v-2Z" />
  </svg>
);

// ─── Component ────────────────────────────────────────────────────────────────

const TOTAL_IMAGES = 348;
const MIN_INDEX_HEIGHT = 200;
const MAX_INDEX_HEIGHT = 700;
const DEFAULT_INDEX_HEIGHT = 340;

export const RegistryViewer = () => {
  // 1. Detect if this specific window is the spawned "IndexPanel" window
  const isDetachedMode = typeof window !== 'undefined' && 
    (window.location.search.includes('mode=index') || window.location.hash.includes('mode=index'));

  const [currentImage,   setCurrentImage]   = createSignal(12);
  const [indexVisible,   setIndexVisible]   = createSignal(true);
  const [isDetached,     setIsDetached]     = createSignal(false);
  const [indexHeight,    setIndexHeight]    = createSignal(DEFAULT_INDEX_HEIGHT);
  const [selectedActId,  setSelectedActId]  = createSignal<number | null>(3);
  const [notes,          setNotes]          = createSignal("");
  const [saveStatus,     setSaveStatus]     = createSignal<"saved" | "saving" | "error">("saved");

  // 2. Setup Cross-Window State Sync via BroadcastChannel
  const channel = typeof window !== 'undefined' ? new BroadcastChannel('geneagrab_sync') : null;

  onMount(() => {
    if (!channel) return;

    if (isDetachedMode) {
      // We are the detached popup. Tell the main window we are ready.
      channel.postMessage({ type: 'READY' });
      
      channel.onmessage = (e) => {
        if (e.data.type === 'SYNC_STATE') setSelectedActId(e.data.selectedActId);
      };
      
      // Fallback for normal browsers closing
      window.addEventListener('beforeunload', () => channel.postMessage({ type: 'DETACHED_CLOSED' }));
    } else {
      // We are the Main Window. Listen for updates from the detached popup.
      channel.onmessage = (e) => {
        if (e.data.type === 'READY') {
          // Send initial state to popup immediately
          channel.postMessage({ type: 'SYNC_STATE', selectedActId: selectedActId() });
        }
        if (e.data.type === 'DETACHED_CLOSED' || e.data.type === 'TOGGLE_DETACHED') {
          setIsDetached(false);
        }
        if (e.data.type === 'SELECT_ACT') {
          setSelectedActId(e.data.id);
        }
      };
    }
  });

  onCleanup(() => {
    if (channel) channel.close();
  });

  // 3. Whenever selectedActId changes in the main window, broadcast it
  createEffect(() => {
    if (!isDetachedMode && channel) {
      channel.postMessage({ type: 'SYNC_STATE', selectedActId: selectedActId() });
    }
  });

  const selectedAct = () => selectedActId() === MOCK_ACT.id ? MOCK_ACT : null;

  // Handles closing the detached window via button click
  const handleCloseDetachedWindow = async () => {
    channel?.postMessage({ type: 'TOGGLE_DETACHED' });
    const isTauri = typeof window !== 'undefined' && ("__TAURI_INTERNALS__" in window || "__TAURI__" in window);
    
    if (isTauri) {
      try {
        const { getCurrentWindow } = await import('@tauri-apps/api/window');
        await getCurrentWindow().close();
        return;
      } catch (e) {
        console.warn("Tauri close API not available, falling back to window.close", e);
      }
    }
    window.close();
  };

  // ── Early Return for Detached Native Window ──
  if (isDetachedMode) {
    return (
      <div 
        class="w-screen h-screen overflow-hidden flex flex-col bg-white text-[#2c2820] antialiased"
        style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}
      >
        <IndexPanel
          visible={true}
          isDetached={true}
          height={0} // Irrelevant when flex-1 full width
          rows={MOCK_ROWS}
          selectedActId={selectedActId()}
          selectedAct={selectedAct()}
          indexedCount={2}
          onToggle={handleCloseDetachedWindow}
          onDetach={handleCloseDetachedWindow}
          onSelectRow={(row) => channel?.postMessage({ type: 'SELECT_ACT', id: row.id })}
          onNewAct={() => console.log("new act")}
          onSave={(act) => console.log("save", act)}
          onValidateAndNext={(act) => console.log("validate", act)}
          onReset={() => console.log("reset")}
        />
      </div>
    );
  }

  // ── Spawn Window Logic ──
  const handleDetach = async () => {
    setIsDetached(true);
    
    const urlObj = new URL(window.location.href);
    urlObj.searchParams.set('mode', 'index');
    const targetUrl = urlObj.pathname + urlObj.search + urlObj.hash;

    const isTauri = typeof window !== 'undefined' && ("__TAURI_INTERNALS__" in window || "__TAURI__" in window);
    
    if (isTauri) {
      try {
        const { WebviewWindow } = await import('@tauri-apps/api/webviewWindow');
        
        const windowLabel = `index-window-${Date.now()}`;
        const webview = new WebviewWindow(windowLabel, {
          url: targetUrl,
          title: 'Index - GeneaGrab',
          width: 900,
          height: 600,
          x: 200,
          y: 200,
        });

        webview.once('tauri://error', (e: any) => {
          console.error('Tauri window creation error:', e);
          setIsDetached(false);
        });

        // CRITICAL: Native event listener for when user closes window using OS 'X' button
        webview.once('tauri://destroyed', () => {
          setIsDetached(false);
        });
        
        return;
      } catch (e: any) {
        console.warn("Failed to spawn Tauri native window.", e);
        setIsDetached(false);
      }
    }

    // Fallback for standard browsers
    const popup = window.open(targetUrl, 'IndexWindow', 'width=900,height=600,left=200,top=200');
    if (popup) {
      // Poll to catch window closing in browsers bypassing beforeunload
      const timer = setInterval(() => {
        if (popup.closed) {
          clearInterval(timer);
          setIsDetached(false);
        }
      }, 500);
    } else {
      setIsDetached(false);
    }
  };

  // ── Global keyboard shortcuts ─────────────────────────────────────────────
  const onKeyDown = (e: KeyboardEvent) => {
    const target = e.target as HTMLElement;
    const inInput = target.tagName === "INPUT" || target.tagName === "TEXTAREA";

    if (!inInput) {
      if (e.key === "ArrowLeft")  setCurrentImage(p => Math.max(1, p - 1));
      if (e.key === "ArrowRight") setCurrentImage(p => Math.min(TOTAL_IMAGES, p + 1));
    }
    if (e.key === "i" && e.ctrlKey) { e.preventDefault(); setIndexVisible(v => !v); }
    if (e.key === "s" && e.ctrlKey && inInput) { e.preventDefault(); /* trigger save */ }
  };

  onMount(() => window.addEventListener("keydown", onKeyDown));
  onCleanup(() => window.removeEventListener("keydown", onKeyDown));

  // ── Panel resize (viewer ↔ index) ─────────────────────────────────────────
  const onResizePointerDown = (e: PointerEvent) => {
    e.preventDefault();
    const startY = e.clientY;
    const startH = indexHeight();

    const onMove = (ev: PointerEvent) => {
      const delta = startY - ev.clientY;
      setIndexHeight(Math.max(MIN_INDEX_HEIGHT, Math.min(MAX_INDEX_HEIGHT, startH + delta)));
    };
    const onUp = () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  };

  // ── Notes auto-save ───────────────────────────────────────────────────────
  let saveTimer: ReturnType<typeof setTimeout>;
  const handleNotesChange = (value: string) => {
    setNotes(value);
    setSaveStatus("saving");
    clearTimeout(saveTimer);
    saveTimer = setTimeout(() => setSaveStatus("saved"), 800);
  };
  onCleanup(() => clearTimeout(saveTimer));


  return (
    <div
      class="flex flex-col w-screen h-screen overflow-hidden bg-[#f7f3ee] text-[#2c2820] select-none antialiased"
      style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}
    >
      {/* ── Titlebar (Tauri custom drag region) ── */}
      <div
        data-tauri-drag-region
        class="flex-shrink-0 flex items-center justify-between px-4 h-11 bg-white border-b border-[#e0d8cc] shadow-sm"
      >
        {/* Breadcrumb */}
        <div class="flex items-center gap-2 min-w-0 overflow-hidden pointer-events-none">
          <span class="text-[15px] font-bold text-[#b8743a] flex-shrink-0">GeneaGrab</span>
          <svg class="w-3.5 h-3.5 text-[#ccc4b8] flex-shrink-0" viewBox="0 0 16 16" fill="currentColor">
            <path d="M6 3l5 5-5 5V3Z" />
          </svg>
          <span class="text-[13px] text-[#a89e93] flex-shrink-0">AD83 · 5 Mi 1/342</span>
          <svg class="w-3.5 h-3.5 text-[#ccc4b8] flex-shrink-0" viewBox="0 0 16 16" fill="currentColor">
            <path d="M6 3l5 5-5 5V3Z" />
          </svg>
          <span class="text-[13px] text-[#2c2820] font-medium truncate">
            Brignoles — Naissances 1792–1832
          </span>
        </div>

        {/* Index toggle */}
        <button
          type="button"
          onClick={() => setIndexVisible(v => !v)}
          aria-pressed={indexVisible()}
          class={[
            "flex items-center gap-2 px-3 py-1.5 rounded-lg",
            "text-[13px] font-medium border transition-colors duration-100 flex-shrink-0",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#b8743a]",
            indexVisible()
              ? "bg-[#fef3e7] border-[#f0c990] text-[#7a4a1e]"
              : "bg-white border-[#e0d8cc] text-[#6b6358] hover:bg-[#faf7f3]",
          ].join(" ")}
        >
          <IndexIcon />
          Index
          <Kbd>Ctrl I</Kbd>
        </button>
      </div>

      {/* ── Viewer workspace ── */}
      <div class="flex flex-1 min-h-0 overflow-hidden">
        {/* Viewer column */}
        <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
          <MainViewer
            currentImage={currentImage()}
            totalImages={TOTAL_IMAGES}
            onImageChange={setCurrentImage}
          />
          <ThumbnailBar
            totalImages={TOTAL_IMAGES}
            currentImage={currentImage()}
            onImageChange={setCurrentImage}
          />
        </div>

        {/* Right panel */}
        <InfoNotesPanel
          registryMeta={MOCK_REGISTRY}
          imageMeta={MOCK_IMAGE}
          image={currentImage().toString()}
          notes={notes()}
          onNotesChange={handleNotesChange}
          saveStatus={saveStatus()}
        />
      </div>

      {/* ── Vertical resize handle above IndexPanel ── */}
      {indexVisible() && !isDetached() && (
        <div
          class={[
            "flex-shrink-0 h-[6px] w-full cursor-row-resize z-10 group",
            "bg-[#ede8e1] hover:bg-[#b8743a]/25 active:bg-[#b8743a]/50",
            "transition-colors duration-150 flex items-center justify-center",
          ].join(" ")}
          onPointerDown={onResizePointerDown}
          role="separator"
          aria-orientation="horizontal"
          aria-label="Redimensionner le panneau d'index"
        >
          <div class="flex flex-row gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
            {[0, 1, 2].map(() => <div class="w-1 h-1 rounded-full bg-[#b8743a]" />)}
          </div>
        </div>
      )}

      {/* ── Inline Index Panel ── */}
      {indexVisible() && !isDetached() && (
        <IndexPanel
          visible={true}
          isDetached={false}
          height={indexHeight()}
          rows={MOCK_ROWS}
          selectedActId={selectedActId()}
          selectedAct={selectedAct()}
          indexedCount={2}
          onToggle={() => setIndexVisible(false)}
          onDetach={handleDetach}
          onSelectRow={(row) => setSelectedActId(row.id)}
          onNewAct={() => console.log("new act")}
          onSave={(act) => console.log("save", act)}
          onValidateAndNext={(act) => console.log("validate", act)}
          onReset={() => console.log("reset")}
        />
      )}

      {/* ── Status bar ── */}
      <div
        class="flex-shrink-0 flex items-center justify-between px-4 h-6 bg-white border-t border-[#e0d8cc]"
        role="status"
        aria-live="polite"
      >
        <div class="flex items-center gap-4">
          <span class="text-[11px] text-[#a89e93]">{currentImage()}</span>
          <span class="text-[11px] text-[#a89e93]">{TOTAL_IMAGES} prises de vue · {MOCK_ROWS.length} actes indexés</span>
          <span class="text-[11px] text-[#3a8c5c] flex items-center gap-1">
            <span class="w-1.5 h-1.5 rounded-full bg-[#3a8c5c] inline-block" />
            AD83 connecté
          </span>
        </div>
        <div class="flex items-center gap-3">
          <span class="text-[11px] text-[#a89e93] tabular-nums">
            {new Date().toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" })}
          </span>
        </div>
      </div>
    </div>
  );
};

export default RegistryViewer;
