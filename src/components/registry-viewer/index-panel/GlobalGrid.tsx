import { For } from "solid-js";
import type { EventRow } from "../../../types/registry";
import { Button } from "../../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { TranslationKey, useI18n } from "../../../ui/i18n";
import { useRegistryActions } from "../../../contexts/RegistryActionsContext";

const EVENT_CHIP: Record<string, string> = {
  Naissance: "text-event-birth bg-event-birth-bg",
  Mariage: "text-event-marriage bg-event-marriage-bg",
  Décès: "text-event-death bg-event-death-bg",
  Sépulture: "text-event-burial bg-event-burial-bg",
  Testament: "text-event-will bg-event-will-bg",
  Recensement: "text-event-census bg-event-census-bg",
  Autre: "text-event-other bg-event-other-bg",
};

interface GlobalGridProps {
  rows: EventRow[];
  selectedId: number | null;
  onSelect: (row: EventRow) => void;
  onRef?: (el: HTMLDivElement) => void;
  onFocusDetail?: () => void;
}

export const GlobalGrid = (props: GlobalGridProps) => {
  const { t } = useI18n();
  const actions = useRegistryActions(); // Consume the context directly

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

  const headers: TranslationKey[] = ["grid.number", "grid.date", "grid.type", "grid.title"];
  return (
    <div class="flex flex-col h-full border-r border-subtle bg-panel flex-shrink-0" style={{ "min-width": "240px" }} ref={props.onRef}>
      {/* Column headers */}
      <div class="flex-shrink-0 grid px-3 py-2 border-b border-subtle bg-tinted" style={{ "grid-template-columns": "2.5rem 6.5rem 5rem 1fr" }} aria-hidden="true">
        <For each={headers}>{col => (
          <span class="text-[11px] font-semibold uppercase tracking-wider text-dim">{t(col)}</span>
        )}
        </For>
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
                  "w-full grid px-3 py-2 text-left border-b border-hover transition-colors duration-75 focus-visible:outline-none focus-visible:ring-inset focus-visible:ring-2 focus-visible:ring-accent",
                  isSelected() ? "bg-accent-bg border-l-[3px] border-l-accent" : "hover:bg-tinted border-l-[3px] border-l-transparent",
                ].join(" ")}
                style={{ "grid-template-columns": "2.5rem 6.5rem 5rem 1fr" }}
              >
                <span class="text-[12px] text-dim tabular-nums">{row.event_id}</span>
                <span class="text-[13px] text-muted tabular-nums truncate">{row.date}</span>
                <span class={["text-[10px] font-medium px-1.5 py-0.5 rounded-full w-fit leading-none self-center", EVENT_CHIP[row.event_type || "Autre"] || EVENT_CHIP["Autre"]].join(" ")}>
                  {(row.event_type || "Autre").substring(0, 5)}...
                </span>
                <span class="text-[13px] text-main truncate font-medium">{row.title || "-"}</span>
              </button>
            );
          }}
        </For>
      </div>

      {/* Footer */}
      <div class="flex-shrink-0 flex items-center justify-between px-3 py-2 border-t border-subtle bg-tinted">
        <span class="text-[12px] text-dim">{t("grid.actsCount", { count: props.rows.length })}</span>
        <Button variant="outline" size="sm" onClick={actions.onNewAct}><Icon icon="lucide:plus" width="16" height="16" /> {t("grid.newAct")}</Button>
      </div>
    </div>
  );
};
