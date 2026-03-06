import { createSignal } from "solid-js";
import { IconButton, Divider, Button } from "../../ui/primitives";

const ChevronLeft = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M10 3 5 8l5 5V3Z" />
  </svg>
);
const ChevronRight = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M6 3l5 5-5 5V3Z" />
  </svg>
);
const ZoomInIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M7 3v4H3v2h4v4h2V9h4V7H9V3H7Z" />
  </svg>
);
const ZoomOutIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M3 7h10v2H3V7Z" />
  </svg>
);
const FitIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M1 1h5v2H3v3H1V1Zm9 0h5v5h-2V3h-3V1ZM1 10h2v3h3v2H1v-5Zm13 3h-3v2h5v-5h-2v3Z" />
  </svg>
);
const RotateIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M13 5A6 6 0 1 0 8 14v-2a4 4 0 1 1 3.7-2.5L10 8h4V4l-1.3 1.3A5.97 5.97 0 0 0 13 5Z" />
  </svg>
);
const DownloadIcon = () => (
  <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
    <path d="M8 10 4 6h2.5V2h3v4H12L8 10Zm-5 2h10v2H3v-2Z" />
  </svg>
);

interface MainViewerProps {
  currentPage: number;
  totalPages: number;
  imageSrc?: string;
  onPageChange: (page: number) => void;
}

export const MainViewer = (props: MainViewerProps) => {
  const [zoom, setZoom] = createSignal(100);
  const [rotation, setRotation] = createSignal(0);
  const [pageInput, setPageInput] = createSignal(String(props.currentPage));

  const clamp = (z: number) => Math.max(10, Math.min(400, z));

  const commitPageInput = () => {
    const n = parseInt(pageInput(), 10);
    if (!isNaN(n) && n >= 1 && n <= props.totalPages) {
      props.onPageChange(n);
    } else {
      setPageInput(String(props.currentPage));
    }
  };

  return (
    <div class="relative flex-1 flex flex-col overflow-hidden bg-[#eee8df] min-h-0">

      {/* ── Toolbar ── */}
      <div class="flex-shrink-0 flex items-center gap-1 px-3 py-2 bg-white border-b border-[#e0d8cc] shadow-sm">

        {/* Page navigation */}
        <IconButton title="Page précédente (←)" onClick={() => props.onPageChange(Math.max(1, props.currentPage - 1))}>
          <ChevronLeft />
        </IconButton>

        {/* Editable page input */}
        <div class="flex items-center gap-1.5 mx-1">
          <input
            type="text"
            value={pageInput()}
            onInput={(e) => setPageInput(e.currentTarget.value)}
            onBlur={commitPageInput}
            onKeyDown={(e) => e.key === "Enter" && commitPageInput()}
            class={[
              "w-12 h-8 text-center rounded-md border border-[#e0d8cc]",
              "text-[14px] text-[#2c2820] bg-[#faf7f3]",
              "focus:outline-none focus:border-[#b8743a] focus:ring-2 focus:ring-[#b8743a]/20",
              "transition-all tabular-nums",
            ].join(" ")}
            aria-label="Numéro de page"
          />
          <span class="text-[13px] text-[#a89e93] select-none">
            / {props.totalPages}
          </span>
        </div>

        <IconButton title="Page suivante (→)" onClick={() => props.onPageChange(Math.min(props.totalPages, props.currentPage + 1))}>
          <ChevronRight />
        </IconButton>

        <Divider vertical class="mx-2 h-5" />

        {/* Zoom */}
        <IconButton title="Dézoomer (−)" onClick={() => setZoom(z => clamp(z - 10))}>
          <ZoomOutIcon />
        </IconButton>
        <span class="text-[13px] text-[#6b6358] tabular-nums w-10 text-center select-none">
          {zoom()}%
        </span>
        <IconButton title="Zoomer (+)" onClick={() => setZoom(z => clamp(z + 10))}>
          <ZoomInIcon />
        </IconButton>
        <IconButton title="Ajuster à la fenêtre (F)" onClick={() => setZoom(100)}>
          <FitIcon />
        </IconButton>
        <IconButton title="Pivoter 90° (R)" onClick={() => setRotation(r => (r + 90) % 360)}>
          <RotateIcon />
        </IconButton>

        <Divider vertical class="mx-2 h-5" />

        <IconButton title="Télécharger l'image HD">
          <DownloadIcon />
        </IconButton>
      </div>

      {/* ── Canvas ── */}
      <div class="flex-1 overflow-auto flex items-center justify-center min-h-0 p-6">
        {props.imageSrc ? (
          <img
            src={props.imageSrc}
            alt={`Folio ${props.currentPage}`}
            class="shadow-2xl shadow-black/20 object-contain max-h-full rounded-sm"
            style={{
              width: `${zoom()}%`,
              transform: `rotate(${rotation()}deg)`,
              transition: "transform 0.2s ease",
            }}
          />
        ) : (
          <div
            class="relative bg-white border border-[#e0d8cc] shadow-xl shadow-black/10 rounded-sm flex items-center justify-center overflow-hidden"
            style={{
              width: `${zoom() * 4.4}px`,
              height: `${zoom() * 5.6}px`,
              transform: `rotate(${rotation()}deg)`,
              transition: "transform 0.2s ease",
              "max-width": "none",
            }}
          >
            <div
              class="absolute inset-0 opacity-[0.07]"
              style={{ background: "repeating-linear-gradient(0deg, transparent, transparent 27px, #b8743a 27px, #b8743a 28px)" }}
            />
            <div class="flex flex-col items-center gap-3 select-none">
              <svg class="w-10 h-10 text-[#e0d8cc]" viewBox="0 0 24 24" fill="currentColor">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6Zm4 18H6V4h7v5h5v11Z" />
              </svg>
              <span class="text-[13px] text-[#a89e93]">f.{props.currentPage}r — aucune image</span>
            </div>
            <span class="absolute bottom-3 right-4 text-[12px] text-[#e0d8cc] select-none font-mono">
              {props.currentPage}r
            </span>
          </div>
        )}
      </div>
    </div>
  );
};
