import { createResource, Show } from "solid-js";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { useBackend } from "../../contexts/BackendContext";
import { ACT_TYPE_OPTIONS } from "../../types/registry";

interface RegistryFiltersProps {
  width?: number;
  searchQuery: string;
  onSearchChange: (val: string) => void;
  selectedType: string;
  onTypeChange: (val: string) => void;
  selectedPlace: string;
  onPlaceChange: (val: string) => void;
  selectedCollection: string;
  onCollectionChange: (val: string) => void;
  dateFrom: string;
  onDateFromChange: (val: string) => void;
  dateTo: string;
  onDateToChange: (val: string) => void;
}

export const RegistryFilters = (props: RegistryFiltersProps) => {
  const { t } = useI18n();
  const api = useBackend();

  const [places] = createResource(() => api.getAvailablePlaces());
  const [collections] = createResource(() => api.getAvailableCollections());

  const activeFiltersCount = () => {
    return [props.selectedType, props.selectedPlace, props.selectedCollection, props.dateFrom, props.dateTo].filter(Boolean).length;
  };

  const hasAnyFilter = () => {
    return activeFiltersCount() > 0 || props.searchQuery.trim().length > 0;
  };

  const handleClearAll = () => {
    props.onSearchChange("");
    props.onTypeChange("");
    props.onPlaceChange("");
    props.onCollectionChange("");
    props.onDateFromChange("");
    props.onDateToChange("");
  };

  return (
    <aside
      class="flex flex-col flex-shrink-0 bg-panel border-r border-subtle overflow-hidden h-full select-none"
      style={{ width: `${props.width ?? 280}px` }}
      aria-label={t("home.filters")}
    >
      {/* Filter controls */}
      <div class="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-5 min-h-0 select-text scrollbar-thin scrollbar-thumb-subtle">
        {/* Search */}
        <div class="flex flex-col gap-1.5">
          <div class="flex items-center justify-between">
            <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
              {t("home.searchLabel")}
            </label>
            <Show when={hasAnyFilter()}>
              <button
                type="button"
                onClick={handleClearAll}
                class="text-[11px] text-accent hover:text-accent-hover font-medium transition-colors cursor-pointer"
              >
                {t("home.clearFilters")}
              </button>
            </Show>
          </div>
          <div class="relative w-full">
            <Icon icon="lucide:search" class="absolute left-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4 pointer-events-none" />
            <input
              type="text"
              value={props.searchQuery}
              onInput={(e) => props.onSearchChange(e.currentTarget.value)}
              placeholder={t("home.searchPlaceholder")}
              class="w-full pl-9 pr-8 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all"
            />
            <Show when={props.searchQuery}>
              <button
                type="button"
                onClick={() => props.onSearchChange("")}
                class="absolute right-2 top-1/2 -translate-y-1/2 text-subtle-md hover:text-main transition-colors p-1 rounded cursor-pointer"
              >
                <Icon icon="lucide:x" class="w-3.5 h-3.5" />
              </button>
            </Show>
          </div>
        </div>

        {/* Place */}
        <div class="flex flex-col gap-1.5">
          <div class="flex items-center justify-between">
            <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
              {t("home.placesLabel")}
            </label>
            <Show when={props.selectedPlace}>
              <button
                type="button"
                onClick={() => props.onPlaceChange("")}
                class="text-[11px] text-muted hover:text-main cursor-pointer"
              >
                <Icon icon="lucide:x" class="w-3 h-3" />
              </button>
            </Show>
          </div>
          <div class="relative">
            <select
              value={props.selectedPlace}
              onChange={(e) => props.onPlaceChange(e.currentTarget.value)}
              disabled={places.loading}
              class="w-full pl-3 pr-8 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all appearance-none cursor-pointer disabled:opacity-50"
            >
              <option value="">{t("home.filterPlace")}</option>
              <Show when={places()}>{(list) => list().map((opt) => <option value={opt}>{opt}</option>)}</Show>
            </select>
            <Icon icon="lucide:chevron-down" class="absolute right-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4 pointer-events-none" />
          </div>
        </div>

        {/* Collection */}
        <div class="flex flex-col gap-1.5">
          <div class="flex items-center justify-between">
            <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
              {t("home.collectionsLabel")}
            </label>
            <Show when={props.selectedCollection}>
              <button
                type="button"
                onClick={() => props.onCollectionChange("")}
                class="text-[11px] text-muted hover:text-main cursor-pointer"
              >
                <Icon icon="lucide:x" class="w-3 h-3" />
              </button>
            </Show>
          </div>
          <div class="relative">
            <select
              value={props.selectedCollection}
              onChange={(e) => props.onCollectionChange(e.currentTarget.value)}
              disabled={collections.loading}
              class="w-full pl-3 pr-8 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all appearance-none cursor-pointer disabled:opacity-50"
            >
              <option value="">{t("home.filterCollection")}</option>
              <Show when={collections()}>{(list) => list().map((opt) => <option value={opt}>{opt}</option>)}</Show>
            </select>
            <Icon icon="lucide:chevron-down" class="absolute right-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4 pointer-events-none" />
          </div>
        </div>

        {/* Record Type */}
        <div class="flex flex-col gap-1.5">
          <div class="flex items-center justify-between">
            <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
              {t("home.typesLabel")}
            </label>
            <Show when={props.selectedType}>
              <button
                type="button"
                onClick={() => props.onTypeChange("")}
                class="text-[11px] text-muted hover:text-main cursor-pointer"
              >
                <Icon icon="lucide:x" class="w-3 h-3" />
              </button>
            </Show>
          </div>
          <div class="relative">
            <select
              value={props.selectedType}
              onChange={(e) => props.onTypeChange(e.currentTarget.value)}
              class="w-full pl-3 pr-8 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all appearance-none cursor-pointer"
            >
              <option value="">{t("home.filterType")}</option>
              {ACT_TYPE_OPTIONS.map((opt) => (
                <option value={opt}>{t(`actCategories.${opt}` as any) || opt}</option>
              ))}
            </select>
            <Icon icon="lucide:chevron-down" class="absolute right-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4 pointer-events-none" />
          </div>
        </div>

        {/* Date Range */}
        <div class="flex flex-col gap-1.5">
          <div class="flex items-center justify-between">
            <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
              {t("home.datesLabel")}
            </label>
            <Show when={props.dateFrom || props.dateTo}>
              <button
                type="button"
                onClick={() => {
                  props.onDateFromChange("");
                  props.onDateToChange("");
                }}
                class="text-[11px] text-muted hover:text-main cursor-pointer"
              >
                <Icon icon="lucide:x" class="w-3 h-3" />
              </button>
            </Show>
          </div>
          <div class="grid grid-cols-2 gap-2">
            <div class="relative">
              <input
                type="number"
                value={props.dateFrom}
                onInput={(e) => props.onDateFromChange(e.currentTarget.value)}
                placeholder={t("home.filterDateFrom")}
                class="w-full px-3 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all"
              />
            </div>
            <div class="relative">
              <input
                type="number"
                value={props.dateTo}
                onInput={(e) => props.onDateToChange(e.currentTarget.value)}
                placeholder={t("home.filterDateTo")}
                class="w-full px-3 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all"
              />
            </div>
          </div>
        </div>
      </div>
    </aside>
  );
};
