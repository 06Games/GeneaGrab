import { createSignal, Show, For } from "solid-js";
import { createVirtualizer } from "@tanstack/solid-virtual";
import { createSolidTable, getCoreRowModel, getSortedRowModel, flexRender, ColumnDef, SortingState } from "@tanstack/solid-table";

import type { EventRow } from "../../../types/index";
import { Button } from "../../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../../ui/i18n";
import { useRegistryActions } from "../../../contexts/RegistryActionsContext";
import { ActTypeCategory } from "../../../types/registry";

const EVENT_CHIP: Record<ActTypeCategory, string> = {
  Vital: "text-event-vital bg-event-vital-bg",
  Union: "text-event-union bg-event-union-bg",
  Mortality: "text-event-mortality bg-event-mortality-bg",
  Census: "text-event-census bg-event-census-bg",
  Legal: "text-event-legal bg-event-legal-bg",
  Land: "text-event-land bg-event-land-bg",
  Media: "text-event-media bg-event-media-bg",
  Military: "text-event-military bg-event-military-bg",
  Other: "text-event-other bg-event-other-bg",
  Unknown: "text-event-other bg-event-other-bg",
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
  const actions = useRegistryActions();
  let scrollRef!: HTMLDivElement;

  const [sorting, setSorting] = createSignal<SortingState>([]);

  // Define Table Columns
  const columns: ColumnDef<EventRow>[] = [
    { accessorKey: "event_id", header: () => t("grid.number") },
    { accessorKey: "date", header: () => t("grid.date") },
    { accessorKey: "event_type", header: () => t("grid.type") },
    { accessorKey: "title", header: () => t("grid.title") },
  ];

  // Initialize Solid Table
  const table = createSolidTable({
    get data() {
      return props.rows;
    },
    columns,
    state: {
      get sorting() {
        return sorting();
      },
    },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  // Initialize Virtualizer
  const virtualizer = createVirtualizer({
    get count() {
      return table.getRowModel().rows.length;
    },
    getScrollElement: () => scrollRef,
    estimateSize: () => 41, // Approx height of each row
    overscan: 10,
  });

  const handleKeyDown = (e: KeyboardEvent) => {
    const sortedRows = table.getRowModel().rows.map((r) => r.original);
    const idx = sortedRows.findIndex((r) => r.event_id === props.selectedId);

    if (e.key === "ArrowUp") {
      e.preventDefault();
      if (idx > 0) props.onSelect(sortedRows[idx - 1]);
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      if (idx < sortedRows.length - 1) props.onSelect(sortedRows[idx + 1]);
    } else if (e.key === "ArrowRight" || e.key === "Tab") {
      if (e.key === "Tab" && e.shiftKey) return;
      e.preventDefault();
      props.onFocusDetail?.();
    }
  };

  return (
    <div class="flex flex-col h-full border-r border-subtle bg-panel flex-shrink-0" style={{ "min-width": "240px" }} ref={props.onRef}>
      {/* Column Headers */}
      <div
        class="flex-shrink-0 grid px-3 py-2 border-b border-subtle bg-tinted"
        style={{ "grid-template-columns": "2.5rem 6.5rem 5rem 1fr" }}
        aria-hidden="true"
      >
        <For each={table.getHeaderGroups()[0].headers}>
          {(header) => (
            <div
              class="text-[11px] font-semibold uppercase tracking-wider text-dim cursor-pointer select-none flex items-center gap-1 hover:text-main transition-colors"
              onClick={header.column.getToggleSortingHandler()}
            >
              {flexRender(header.column.columnDef.header, header.getContext())}
              <Show when={header.column.getIsSorted()}>
                {(sortDir) => <Icon icon={sortDir() === "asc" ? "lucide:arrow-up" : "lucide:arrow-down"} class="w-3 h-3 text-accent" />}
              </Show>
            </div>
          )}
        </For>
      </div>

      {/* Rows */}
      <div class="flex-1 overflow-y-auto focus-visible:outline-none" role="listbox" tabIndex={0} onKeyDown={handleKeyDown} ref={scrollRef}>
        <div style={{ height: `${virtualizer.getTotalSize()}px`, width: "100%", position: "relative" }}>
          <For each={virtualizer.getVirtualItems()}>
            {(virtualRow) => {
              const event = () => table.getRowModel().rows[virtualRow.index].original;
              const isSelected = () => props.selectedId === event().event_id;

              return (
                <button
                  type="button"
                  role="option"
                  aria-selected={isSelected()}
                  onClick={() => props.onSelect(event())}
                  class={[
                    "w-full grid px-3 py-2 text-left border-b border-hover transition-colors duration-75 focus-visible:outline-none focus-visible:ring-inset focus-visible:ring-2 focus-visible:ring-accent items-center",
                    isSelected() ? "bg-accent-bg border-l-[3px] border-l-accent" : "hover:bg-tinted border-l-[3px] border-l-transparent",
                  ].join(" ")}
                  style={{
                    "grid-template-columns": "2.5rem 6.5rem 5rem 1fr",
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    transform: `translateY(${virtualRow.start}px)`,
                    height: `${virtualRow.size}px`,
                  }}
                >
                  <span class="text-[12px] text-dim tabular-nums">{event().event_id}</span>
                  <span class="text-[13px] text-muted tabular-nums truncate">{event().date}</span>
                  <span
                    class={[
                      "text-[10px] font-medium px-1.5 py-0.5 rounded-full w-fit leading-none self-center",
                      EVENT_CHIP[event().event_type.category || "Other"] || EVENT_CHIP["Other"],
                    ].join(" ")}
                  >
                    {(event().event_type.label || "Other").substring(0, 5)}...
                  </span>
                  <span class="text-[13px] text-main truncate font-medium">{event().title || "-"}</span>
                </button>
              );
            }}
          </For>
        </div>
      </div>

      {/* Footer */}
      <div class="flex-shrink-0 flex items-center justify-between px-3 py-2 border-t border-subtle bg-tinted">
        <span class="text-[12px] text-dim">{t("grid.actsCount", { count: props.rows.length })}</span>
        <Button variant="outline" size="sm" onClick={actions.onNewAct}>
          <Icon icon="lucide:plus" width="16" height="16" /> {t("grid.newAct")}
        </Button>
      </div>
    </div>
  );
};
