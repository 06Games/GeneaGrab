import { createSignal, createEffect, onCleanup, onMount } from "solid-js";
import type { EventDetail, EventRow, EventType, ImageMeta, RegistryMeta } from "../../types/registry";
import { Kbd } from "../../ui/primitives";
import { MainViewer } from "./MainViewer";
import { ThumbnailBar } from "./ThumbnailBar";
import { InfoNotesPanel } from "./InfoNotesPanel";
import { IndexPanel } from "./index-panel/IndexPanel";
import { Icon } from "@iconify-icon/solid";
import { useDetachedWindow } from "../../hooks/DetachedWindow";

type SyncMessage = 
  | { type: 'READY' }
  | { type: 'SYNC_STATE', selectedEventId: number | null }
  | { type: 'DETACHED_CLOSED' }
  | { type: 'TOGGLE_DETACHED' }
  | { type: 'SELECT_EVENT', id: number | null };

// ─── Mock data ────────────────────────────────────────────────────────────────

const MOCK_REGISTRY: RegistryMeta = {
  source_id: "src-123",
  archive_reference: "5 Mi 1/342",
  source_types: new Set(["Naissance", "Mariage"]),
  town: "Brignoles",
  repository_url: "https://archives.var.fr",
};

const MOCK_IMAGE: ImageMeta = {
  folio:        "12r",
  dateRange:    "3 Frimaire An II",
  actTypes:     new Map<EventType, number>([
    ["Naissance", 2],
    ["Mariage", 1],
    ["Décès", 1],
  ]),
};

const MOCK_ROWS: EventRow[] = [
  { event_id: 1, date: "03 Frim. II", event_type: "Naissance", title: "MARTIN, Jean-Baptiste" },
  { event_id: 2, date: "03 Frim. II", event_type: "Naissance", title: "DUPONT, Marie" },
  { event_id: 3, date: "05 Frim. II", event_type: "Mariage",   title: "ARNAUD, Pierre ∞ BLANC" },
  { event_id: 4, date: "05 Frim. II", event_type: "Décès",     title: "BOYER, Antoinette" },
  { event_id: 5, date: "07 Frim. II", event_type: "Naissance", title: "ISNARD, Louis" },
  { event_id: 6, date: "12 Frim. II", event_type: "Naissance", title: "FABRE, Thérèse" },
  { event_id: 7, date: "14 Frim. II", event_type: "Mariage",   title: "ROUX, Antoine ∞ AUBERT" },
  { event_id: 8, date: "16 Frim. II", event_type: "Décès",     title: "PASCAL, Jean" },
];

const MOCK_EVENT: EventDetail = {
  event_id: 3, date: "05 Frimaire An II", date_normalized: "1793-11-25",
  event_type: "Mariage", title: "Mariage ARNAUD, Pierre ∞ BLANC",
  act_number: "47", page: "12r", image_number: "12",
  town: "Brignoles", parish: "", hamlet: "",
  transcription_text: "", notes: "",
  people: [
    {
      person_id: "p1", role: "Sujet principal",
      first_name: "Pierre", last_name: "ARNAUD", sex: "M", title: "", age: "27 ans",
      is_deceased: false, occupation: "Laboureur", origin_place: "Brignoles", residence_place: "Brignoles",
      sequence_number: "", notes: "", relationship_type: "Époux de", relationship_to: "Marie BLANC"
    },
    {
      person_id: "p2", role: "Sujet principal",
      first_name: "Marie", last_name: "BLANC", sex: "F", title: "", age: "22 ans",
      is_deceased: false, occupation: "", origin_place: "Brignoles", residence_place: "Brignoles",
      sequence_number: "", notes: "", relationship_type: "Épouse de", relationship_to: "Pierre ARNAUD"
    },
  ]
};

// ─── Component ────────────────────────────────────────────────────────────────

const TOTAL_IMAGES = 348;
const MIN_INDEX_HEIGHT = 200;
const MAX_INDEX_HEIGHT = 700;
const DEFAULT_INDEX_HEIGHT = 340;

export const RegistryViewer = () => {
  const urlParams = new URLSearchParams(typeof window !== 'undefined' ? window.location.search : "");
  const isDetachedMode = urlParams.get('mode') === 'index';
  
  const activeRegistryId = urlParams.get('registryId') || MOCK_REGISTRY.source_id;

  const [currentImage,    setCurrentImage]    = createSignal(12);
  const [indexVisible,    setIndexVisible]    = createSignal(true);
  const [indexHeight,     setIndexHeight]     = createSignal(DEFAULT_INDEX_HEIGHT);
  const [selectedEventId, setSelectedEventId] = createSignal<number | null>(3);
  const [notes,           setNotes]           = createSignal("");
  const [saveStatus,      setSaveStatus]      = createSignal<"saved" | "saving" | "error">("saved");

  const { isDetached, setIsDetached, detach, closeSelf, sendMessage } = useDetachedWindow<SyncMessage>({
    id: `index-${activeRegistryId}`,
    title: `Index - ${MOCK_REGISTRY.archive_reference}`,
    queryParams: {
      mode: 'index',
      registryId: activeRegistryId
    },
    width: 1400,
    height: 600,
    onMessage: (msg) => {
      if (isDetachedMode) {
        // We are the popup window receiving a sync from the main window
        if (msg.type === 'SYNC_STATE') setSelectedEventId(msg.selectedEventId);
      } else {
        // We are the main window receiving messages from the popup window
        if (msg.type === 'READY') {
          sendMessage({ type: 'SYNC_STATE', selectedEventId: selectedEventId() });
        }
        if (msg.type === 'DETACHED_CLOSED' || msg.type === 'TOGGLE_DETACHED') {
          setIsDetached(false);
        }
        if (msg.type === 'SELECT_EVENT') {
          setSelectedEventId(msg.id);
        }
      }
    }
  });

  onMount(() => {
    if (isDetachedMode) {
      // Tell the main window we are ready
      sendMessage({ type: 'READY' });
      
      // Fallback for normal browsers closing
      const handleUnload = () => sendMessage({ type: 'DETACHED_CLOSED' });
      window.addEventListener('beforeunload', handleUnload);
      onCleanup(() => window.removeEventListener('beforeunload', handleUnload));
    }
  });

  // Whenever selectedEventId changes in the main window, broadcast it
  createEffect(() => {
    if (!isDetachedMode) {
      sendMessage({ type: 'SYNC_STATE', selectedEventId: selectedEventId() });
    }
  });

  const selectedEvent = () => selectedEventId() === MOCK_EVENT.event_id ? MOCK_EVENT : null;

  const handleCloseDetachedWindow = async () => {
    sendMessage({ type: 'TOGGLE_DETACHED' });
    await closeSelf();
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
          selectedEventId={selectedEventId()}
          selectedEvent={selectedEvent()}
          onToggle={handleCloseDetachedWindow}
          onDetach={handleCloseDetachedWindow}
          onSelectRow={(row) => sendMessage({ type: 'SELECT_EVENT', id: row.event_id })}
          onNewAct={() => console.log("new event")}
          onSave={(event) => console.log("save", event)}
          onValidateAndNext={(event) => console.log("validate", event)}
          onReset={() => console.log("reset")}
        />
      </div>
    );
  }

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
      <div
        data-tauri-drag-region
        class="flex-shrink-0 flex items-center justify-between px-4 h-11 bg-white border-b border-[#e0d8cc] shadow-sm"
      >
          <div class="flex items-center gap-2 min-w-0 overflow-hidden pointer-events-none">
          <span class="text-[15px] font-bold text-[#b8743a] flex-shrink-0">GeneaGrab</span>
          <Icon icon="lucide:chevron-right" class="w-3.5 h-3.5 text-[#ccc4b8] flex-shrink-0"></Icon>
          <span class="text-[13px] text-[#a89e93] flex-shrink-0">AD83 · 5 Mi 1/342</span>
          <Icon icon="lucide:chevron-right" class="w-3.5 h-3.5 text-[#ccc4b8] flex-shrink-0"></Icon>
          <span class="text-[13px] text-[#2c2820] font-medium truncate">
            Brignoles — Naissances 1792–1832
          </span>
        </div>

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
          <Icon icon="lucide:list"></Icon>
          Index
          <Kbd>Ctrl I</Kbd>
        </button>
      </div>

      <div class="flex flex-1 min-h-0 overflow-hidden">
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

        <InfoNotesPanel
          registryMeta={MOCK_REGISTRY}
          imageMeta={MOCK_IMAGE}
          image={currentImage().toString()}
          notes={notes()}
          onNotesChange={handleNotesChange}
          saveStatus={saveStatus()}
        />
      </div>

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

      {indexVisible() && !isDetached() && (
        <IndexPanel
          visible={true}
          isDetached={false}
          height={indexHeight()}
          rows={MOCK_ROWS}
          selectedEventId={selectedEventId()}
          selectedEvent={selectedEvent()}
          onToggle={() => setIndexVisible(false)}
          onDetach={() => detach()}
          onSelectRow={(row) => setSelectedEventId(row.event_id)}
          onNewAct={() => console.log("new event")}
          onSave={(event) => console.log("save", event)}
          onValidateAndNext={(event) => console.log("validate", event)}
          onReset={() => console.log("reset")}
        />
      )}

      <div
        class="flex-shrink-0 flex items-center justify-between px-4 h-6 bg-white border-t border-[#e0d8cc]"
        role="status"
        aria-live="polite"
      >
        <div class="flex items-center gap-4">
          <span class="text-[11px] text-[#a89e93]">{currentImage()}</span>
          <span class="text-[11px] text-[#a89e93]">{TOTAL_IMAGES} prises de vue · {MOCK_ROWS.length} actes indexés</span>
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
