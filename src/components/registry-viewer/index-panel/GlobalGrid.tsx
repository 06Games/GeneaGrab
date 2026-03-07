import { For } from "solid-js";
import type { EventRow, EventType } from "../../../types/registry";
import { Button } from "../../../ui/primitives";
import { Icon } from "@iconify-icon/solid";

const EVENT_CHIP: Record<string, string> = {
  Naissance:      "text-[#2d6a4f] bg-[#d8f3dc]",
  Mariage:        "text-[#1d4e89] bg-[#dbeafe]",
  Décès:          "text-[#7f1d1d] bg-[#fee2e2]",
  Sépulture:      "text-[#451a03] bg-[#fef3c7]",
  Testament:      "text-[#065f46] bg-[#d1fae5]",
  Recensement:    "text-[#5b21b6] bg-[#ede9fe]",
  Autre:          "text-[#6b6358] bg-[#f2ece3]",
};

interface GlobalGridProps {
  rows: EventRow[];
  selectedId: number | null;
  onSelect: (row: EventRow) => void;
  onNewAct: () => void;
  indexedCount: number;
  onRef?: (el: HTMLDivElement) => void;
  onFocusDetail?: () => void;
}

export const GlobalGrid = (props: GlobalGridProps) => {

  const handleKeyDown = (e: KeyboardEvent) => {
    const idx = props.rows.findIndex(r => r.event_id === props.selectedId);
    if (e.key === "ArrowUp") {
      e.preventDefault();
      if (idx > 0) props.onSelect(props.rows[idx - 1]);
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      if (idx < props.rows.length - 1) props.onSelect(props.rows[idx + 1]);
    } else if (e.key === "ArrowRight" || e.key === "Tab") {
      if (e.key === "Tab" && e.shiftKey) return;
      e.preventDefault();
      props.onFocusDetail?.();
    }
  };

  return (
    <div class="flex flex-col h-full border-r border-[#e0d8cc] bg-white flex-shrink-0" style={{ "min-width": "240px" }} ref={props.onRef}>
      {/* Column headers */}
      <div class="flex-shrink-0 grid px-3 py-2 border-b border-[#e0d8cc] bg-[#faf7f3]" style={{ "grid-template-columns": "2.5rem 6.5rem 5rem 1fr" }} aria-hidden="true">
        {["#", "Date", "Type", "Titre"].map(col => (
          <span class="text-[11px] font-semibold uppercase tracking-wider text-[#a89e93]">{col}</span>
        ))}
      </div>

      {/* Rows */}
      <div class="flex-1 overflow-y-auto focus-visible:outline-none" role="listbox" tabIndex={0} onKeyDown={handleKeyDown}>
        <For each={props.rows}>
          {(row) => {
            const isSelected = () => props.selectedId === row.event_id;
            return (
              <button
                type="button" role="option" aria-selected={isSelected()} onClick={() => props.onSelect(row)}
                class={[
                  "w-full grid px-3 py-2 text-left border-b border-[#f2ece3] transition-colors duration-75 focus-visible:outline-none focus-visible:ring-inset focus-visible:ring-2 focus-visible:ring-[#b8743a]",
                  isSelected() ? "bg-[#fef3e7] border-l-[3px] border-l-[#b8743a]" : "hover:bg-[#faf7f3] border-l-[3px] border-l-transparent",
                ].join(" ")}
                style={{ "grid-template-columns": "2.5rem 6.5rem 5rem 1fr" }}
              >
                <span class="text-[12px] text-[#a89e93] tabular-nums">{row.event_id}</span>
                <span class="text-[13px] text-[#6b6358] tabular-nums truncate">{row.date}</span>
                <span class={["text-[10px] font-medium px-1.5 py-0.5 rounded-full w-fit leading-none self-center", EVENT_CHIP[row.event_type || "Autre"] || EVENT_CHIP["Autre"]].join(" ")}>
                  {(row.event_type || "Autre").substring(0, 5)}...
                </span>
                <span class="text-[13px] text-[#2c2820] truncate font-medium">{row.title || "-"}</span>
              </button>
            );
          }}
        </For>
      </div>

      {/* Footer */}
      <div class="flex-shrink-0 flex items-center justify-between px-3 py-2 border-t border-[#e0d8cc] bg-[#faf7f3]">
        <span class="text-[12px] text-[#a89e93]">{props.rows.length} actes · {props.indexedCount} indexés</span>
        <Button variant="outline" size="sm" onClick={props.onNewAct}><Icon icon="lucide:plus" width="16" height="16" /> Nouvel acte</Button>
      </div>
    </div>
  );
};