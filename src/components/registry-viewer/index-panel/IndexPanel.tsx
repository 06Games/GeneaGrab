import { Show, createSignal } from "solid-js";
import type { EventRow, EventDetail } from "../../../types/registry";
import { Badge, IconButton, Kbd } from "../../../ui/primitives";
import { GlobalGrid } from "./GlobalGrid";
import { DetailZone } from "./DetailZone";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../../ui/i18n";

interface IndexPanelProps {
  visible: boolean;
  isDetached?: boolean;
  height: number;
  rows: EventRow[];
  selectedEventId: number | null;
  selectedEvent: EventDetail | null;
  onToggle: () => void;
  onDetach?: () => void;
  onSelectRow: (row: EventRow) => void;
}

const MIN_GRID_WIDTH = 250;
const MAX_GRID_WIDTH = 780;
const DEFAULT_GRID_WIDTH = 480;

export const IndexPanel = (props: IndexPanelProps) => {
  const [gridWidth, setGridWidth] = createSignal(DEFAULT_GRID_WIDTH);
  let gridRef!: HTMLDivElement;
  let detailRef!: HTMLFormElement;
  const { t } = useI18n();

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
      <div class={props.isDetached ? "flex-1 flex flex-col bg-panel w-full h-full" : "flex-shrink-0 flex flex-col border-t border-subtle bg-panel"} style={props.isDetached ? {} : { height: `${props.height}px` }}>
        <div class="flex-shrink-0 flex items-center justify-between px-4 h-10 border-b border-subtle bg-tinted">
          <div class="flex items-center gap-2">
            <span class="text-[13px] font-semibold text-main">{t("indexPanel.title")}</span>
            <Badge>{t("indexPanel.badgeActs", { count: props.rows.length })}</Badge>
          </div>
          <div class="flex items-center gap-1">
            <span class="text-[11px] text-dim mr-1 hidden sm:flex items-center gap-1"><Kbd>→</Kbd> {t("detail.help.detail")} <span class="mx-1">·</span> <Kbd>←</Kbd> {t("detail.help.list")}</span>
            <Show when={!props.isDetached}><IconButton onClick={props.onDetach}><Icon icon="lucide:picture-in-picture"></Icon></IconButton></Show>
            <IconButton onClick={props.onToggle}><Icon icon="lucide:x"></Icon></IconButton>
          </div>
        </div>

        <div class="flex-1 flex min-h-0 overflow-hidden">
          <div class="flex-shrink-0 h-full overflow-hidden" style={{ width: `${gridWidth()}px` }}>
            <GlobalGrid
              rows={props.rows}
              selectedId={props.selectedEventId}
              onSelect={props.onSelectRow}
              onRef={(el) => { gridRef = el; }}
              onFocusDetail={focusDetail}
            />
          </div>

          <div class="flex-shrink-0 w-[6px] h-full cursor-col-resize z-10 group bg-hover hover:bg-accent/25 transition-colors duration-150 flex items-center justify-center" onPointerDown={onHandlePointerDown}>
            <div class="flex flex-col gap-[3px] opacity-0 group-hover:opacity-60 transition-opacity">
              {[0, 1, 2].map(() => <div class="w-1 h-1 rounded-full bg-accent" />)}
            </div>
          </div>

          <DetailZone
            event={props.selectedEvent}
            onRef={(el) => { detailRef = el; }}
            onFocusGrid={focusGrid}
          />
        </div>
      </div>
    </Show>
  );
};
