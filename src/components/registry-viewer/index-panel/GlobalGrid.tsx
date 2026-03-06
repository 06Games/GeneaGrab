import { For } from "solid-js";
import type { ActRow, ActType } from "../../../types/registry";
import { Button } from "../../../ui/primitives";

const ACT_LABEL: Record<ActType, string> = {
  Naissance: "Nais.", Mariage: "Mar.", Décès: "Déc.", Reconnaissance: "Rec.", Autre: "Aut.",
};

const ACT_CHIP: Record<ActType, string> = {
  Naissance:      "text-[#2d6a4f] bg-[#d8f3dc]",
  Mariage:        "text-[#1d4e89] bg-[#dbeafe]",
  Décès:          "text-[#7f1d1d] bg-[#fee2e2]",
  Reconnaissance: "text-[#5b21b6] bg-[#ede9fe]",
  Autre:          "text-[#6b6358] bg-[#f2ece3]",
};

interface GlobalGridProps {
  rows: ActRow[];
  selectedId: number | null;
  onSelect: (row: ActRow) => void;
  onNewAct: () => void;
  indexedCount: number;
  /** Ref callback so parent can programmatically focus the list */
  onRef?: (el: HTMLDivElement) => void;
  /** Called when user presses → or Tab from within this panel */
  onFocusDetail?: () => void;
}

/**
 * GlobalGrid
 * Navigable with ↑/↓ (row movement), Enter (select), → or Tab (go to detail).
 * Production: replace <For> with @tanstack/solid-virtual.
 */
export const GlobalGrid = (props: GlobalGridProps) => {

  const handleKeyDown = (e: KeyboardEvent) => {
    const idx = props.rows.findIndex(r => r.id === props.selectedId);

    if (e.key === "ArrowUp") {
      e.preventDefault();
      if (idx > 0) props.onSelect(props.rows[idx - 1]);
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      if (idx < props.rows.length - 1) props.onSelect(props.rows[idx + 1]);
    } else if (e.key === "ArrowRight" || e.key === "Tab") {
      if (e.key === "Tab" && e.shiftKey) return; // allow shift-tab to exit
      e.preventDefault();
      props.onFocusDetail?.();
    }
  };

  return (
    <div
      class="flex flex-col h-full border-r border-[#e0d8cc] bg-white flex-shrink-0"
      style={{ "min-width": "200px" }}
      ref={props.onRef}
    >
      {/* Column headers */}
      <div
        class="flex-shrink-0 grid px-3 py-2 border-b border-[#e0d8cc] bg-[#faf7f3]"
        style={{ "grid-template-columns": "2.5rem 7rem 4rem 1fr" }}
        aria-hidden="true"
      >
        {["#", "Date", "Type", "Sujet"].map(col => (
          <span class="text-[11px] font-semibold uppercase tracking-wider text-[#a89e93]">{col}</span>
        ))}
      </div>

      {/* Rows */}
      <div
        class="flex-1 overflow-y-auto focus-visible:outline-none"
        role="listbox"
        aria-label="Liste des actes"
        tabIndex={0}
        onKeyDown={handleKeyDown}
      >
        <For each={props.rows}>
          {(row) => {
            const isSelected = () => props.selectedId === row.id;
            return (
              <button
                type="button"
                role="option"
                aria-selected={isSelected()}
                onClick={() => props.onSelect(row)}
                class={[
                  "w-full grid px-3 py-2 text-left border-b border-[#f2ece3]",
                  "transition-colors duration-75",
                  "focus-visible:outline-none focus-visible:ring-inset focus-visible:ring-2 focus-visible:ring-[#b8743a]",
                  isSelected()
                    ? "bg-[#fef3e7] border-l-[3px] border-l-[#b8743a]"
                    : "hover:bg-[#faf7f3] border-l-[3px] border-l-transparent",
                ].join(" ")}
                style={{ "grid-template-columns": "2.5rem 7rem 4rem 1fr" }}
              >
                <span class="text-[12px] text-[#a89e93] tabular-nums">{row.id}</span>
                <span class="text-[13px] text-[#6b6358] tabular-nums truncate">{row.date}</span>
                <span class={[
                  "text-[11px] font-medium px-1.5 py-0.5 rounded-full w-fit leading-none self-center",
                  ACT_CHIP[row.type],
                ].join(" ")}>
                  {ACT_LABEL[row.type]}
                </span>
                <span class="text-[13px] text-[#2c2820] truncate">{row.subject}</span>
              </button>
            );
          }}
        </For>
      </div>

      {/* Footer */}
      <div class="flex-shrink-0 flex items-center justify-between px-3 py-2 border-t border-[#e0d8cc] bg-[#faf7f3]">
        <span class="text-[12px] text-[#a89e93]">
          {props.rows.length} actes · {props.indexedCount} indexés
        </span>
        <Button variant="outline" size="sm" onClick={props.onNewAct}>
          + Nouvel acte
        </Button>
      </div>
    </div>
  );
};
