import { Show, createSignal } from "solid-js";
import type { EventRow, EventDetail } from "../../../types/registry";
import { Badge, IconButton, Kbd } from "../../../ui/primitives";
import { GlobalGrid } from "./GlobalGrid";
import { DetailZone } from "./DetailZone";
import { Icon } from "@iconify-icon/solid";

interface IndexPanelProps {
  visible: boolean;
  isDetached?: boolean;
  height: number;
  rows: EventRow[];
  selectedEventId: number | null;
  selectedEvent: EventDetail | null;
  indexedCount: number;
  onToggle: () => void;
  onDetach?: () => void;
  onSelectRow: (row: EventRow) => void;
  onNewAct: () => void;
  onSave?: (event: EventDetail) => void;
  onValidateAndNext?: (event: EventDetail) => void;
  onReset?: () => void;
}

const MIN_GRID_WIDTH = 480;
const MAX_GRID_WIDTH = 780;
const DEFAULT_GRID_WIDTH = 560;

export const IndexPanel = (props: IndexPanelProps) => {
  const [gridWidth, setGridWidth] = createSignal(DEFAULT_GRID_WIDTH);
  let gridRef!: HTMLDivElement;
  let detailRef!: HTMLDivElement;

  const onHandlePointerDown = (e: PointerEvent) => {
    e.preventDefault();
    const startX  = e.clientX;
    const startW  = gridWidth();
    const onMove = (ev: PointerEvent) => setGridWidth(Math.max(MIN_GRID_WIDTH, Math.min(MAX_GRID_WIDTH, startW + ev.clientX - startX)));
    const onUp = () => { window.removeEventListener("pointermove", onMove); window.removeEventListener("pointerup", onUp); };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  };

  const focusGrid = () => gridRef?.querySelector<HTMLElement>('[role="listbox"]')?.focus();
  const focusDetail = () => detailRef?.querySelector<HTMLInputElement>("input")?.focus();

  return (
    <Show when={props.visible}>
      <div class={props.isDetached ? "flex-1 flex flex-col bg-white w-full h-full" : "flex-shrink-0 flex flex-col border-t border-[#e0d8cc] bg-white"} style={props.isDetached ? {} : { height: `${props.height}px` }}>
        <div class="flex-shrink-0 flex items-center justify-between px-4 h-10 border-b border-[#e0d8cc] bg-[#faf7f3]">
          <div class="flex items-center gap-2">
            <span class="text-[13px] font-semibold text-[#2c2820]">Index</span>
            <Badge>{props.rows.length} actes</Badge>
            <span class="text-[12px] text-[#a89e93]">{props.indexedCount} indexés</span>
          </div>
          <div class="flex items-center gap-1">
            <span class="text-[11px] text-[#a89e93] mr-1 hidden sm:flex items-center gap-1"><Kbd>→</Kbd> détail <span class="mx-1">·</span> <Kbd>←</Kbd> liste</span>
            <Show when={!props.isDetached}><IconButton onClick={props.onDetach}><Icon icon="lucide:picture-in-picture"></Icon></IconButton></Show>
            <IconButton onClick={props.onToggle}><Icon icon="lucide:x"></Icon></IconButton>
          </div>
        </div>

        <div class="flex-1 flex min-h-0 overflow-hidden">
          <div class="flex-shrink-0 h-full overflow-hidden" style={{ width: `${gridWidth()}px` }}>
            <GlobalGrid
              rows={props.rows}
              selectedId={props.selectedEventId}
              indexedCount={props.indexedCount}
              onSelect={props.onSelectRow}
              onNewAct={props.onNewAct}
              onRef={(el) => { gridRef = el; }}
              onFocusDetail={focusDetail}
            />
          </div>

          <div class="flex-shrink-0 w-[6px] h-full cursor-col-resize z-10 group bg-[#f2ece3] hover:bg-[#b8743a]/25 transition-colors duration-150 flex items-center justify-center" onPointerDown={onHandlePointerDown}>
            <div class="flex flex-col gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
              {[0, 1, 2].map(() => <div class="w-1 h-1 rounded-full bg-[#b8743a]" />)}
            </div>
          </div>

          <DetailZone
            event={props.selectedEvent}
            onRef={(el) => { detailRef = el; }}
            onFocusGrid={focusGrid}
            onSave={props.onSave}
            onValidateAndNext={props.onValidateAndNext}
            onReset={props.onReset}
          />
        </div>
      </div>
    </Show>
  );
};
