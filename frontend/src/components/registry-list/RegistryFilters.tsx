import { createEffect, createResource, Show } from "solid-js";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { useBackend } from "../../contexts/BackendContext";
import { ACT_CATEGORY_META, ActTypeCategory } from "../../types/registry";
import { FilterCombobox } from "./FilterCombobox";

interface RegistryFiltersProps {
  width?: number;
  searchQuery: string;
  onSearchChange: (val: string) => void;
  effectiveSearchQuery?: string;
  selectedType: string;
  onTypeChange: (val: string) => void;
  selectedPlace: string;
  onPlaceChange: (val: string) => void;
  selectedCollection: string;
  onCollectionChange: (val: string) => void;
  dateFrom: string;
  onDateFromChange: (val: string) => void;
  effectiveDateFrom?: string;
  dateTo: string;
  onDateToChange: (val: string) => void;
  effectiveDateTo?: string;

  // Optional multi-selection support for future extensibility
  multiPlaces?: boolean;
  selectedPlaces?: string[];
  onPlacesChange?: (vals: string[]) => void;
  multiCollections?: boolean;
  selectedCollections?: string[];
  onCollectionsChange?: (vals: string[]) => void;
  multiTypes?: boolean;
  selectedTypes?: string[];
  onTypesChange?: (vals: string[]) => void;
}

export const RegistryFilters = (props: RegistryFiltersProps) => {
  const { t } = useI18n();
  const api = useBackend();

  const placesFilters = () => ({
    search_term: props.effectiveSearchQuery ?? props.searchQuery,
    source_type: props.selectedType,
    collection: props.selectedCollection,
    date_from: props.effectiveDateFrom ?? props.dateFrom,
    date_to: props.effectiveDateTo ?? props.dateTo,
  });

  const collectionsFilters = () => ({
    search_term: props.effectiveSearchQuery ?? props.searchQuery,
    source_type: props.selectedType,
    place: props.selectedPlace,
    date_from: props.effectiveDateFrom ?? props.dateFrom,
    date_to: props.effectiveDateTo ?? props.dateTo,
  });

  const typesFilters = () => ({
    search_term: props.effectiveSearchQuery ?? props.searchQuery,
    place: props.selectedPlace,
    collection: props.selectedCollection,
    date_from: props.effectiveDateFrom ?? props.dateFrom,
    date_to: props.effectiveDateTo ?? props.dateTo,
  });

  const [places] = createResource(placesFilters, (f) => api.getAvailablePlaces(f));
  const [collections] = createResource(collectionsFilters, (f) => api.getAvailableCollections(f));
  const [types] = createResource(typesFilters, (f) => api.getAvailableTypes(f));

  // Auto-reset selected values if they are no longer available in the updated options
  createEffect(() => {
    const selected = props.selectedPlace;
    const list = places();
    if (!selected || places.loading || !list) return;
    const isValid = list.some((opt) => (opt.key === "__unknown__" ? "__unknown__" : (opt.label || opt.key)) === selected);
    if (!isValid) {
      props.onPlaceChange("");
    }
  });

  createEffect(() => {
    const selected = props.selectedCollection;
    const list = collections();
    if (!selected || collections.loading || !list) return;
    const isValid = list.some((opt) => opt.key === selected);
    if (!isValid) {
      props.onCollectionChange("");
    }
  });

  createEffect(() => {
    const selected = props.selectedType;
    const list = types();
    if (!selected || types.loading || !list) return;
    const isValid = list.some((opt) => opt.key === selected);
    if (!isValid) {
      props.onTypeChange("");
    }
  });

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
    props.onPlacesChange?.([]);
    props.onCollectionsChange?.([]);
    props.onTypesChange?.([]);
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
        <FilterCombobox
          label={t("home.placesLabel")}
          placeholder={t("home.filterPlace")}
          options={places() || []}
          loading={places.loading}
          value={props.selectedPlace}
          onChange={props.onPlaceChange}
          multiple={props.multiPlaces}
          values={props.selectedPlaces}
          onMultipleChange={props.onPlacesChange}
          icon="lucide:map-pin"
          getOptionKey={(opt) => (opt.key === "__unknown__" ? "__unknown__" : (opt.label || opt.key))}
          getOptionLabel={(opt) => (opt.key === "__unknown__" ? t("home.unknownLocation") : opt.label)}
          getSelectedIcon={(opt) =>
            opt.key === "__unknown__"
              ? { icon: "lucide:help-circle", class: "text-muted" }
              : { icon: "lucide:map-pin", class: "text-accent" }
          }
          customFilter={(opt, query) => {
            const q = query.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim();
            if (!q) return true;
            if (opt.key === "__unknown__") {
              return t("home.unknownLocation").toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").includes(q);
            }
            if (opt.label.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").includes(q)) return true;
            if (opt.parts?.some((p) => p.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").includes(q))) return true;
            return false;
          }}
          renderOption={(opt) => {
            if (opt.key === "__unknown__") {
              return (
                <div class="flex items-center gap-2 min-w-0">
                  <Icon icon="lucide:help-circle" class="w-3.5 h-3.5 text-muted flex-shrink-0" />
                  <span class="text-[13px] italic text-muted truncate">
                    {t("home.unknownLocation")}
                  </span>
                </div>
              );
            }
            const parts = opt.parts && opt.parts.length > 0 ? opt.parts : opt.label.split(" > ");
            const leaf = parts[parts.length - 1];
            const hierarchy = parts.slice(0, parts.length - 1);
            return (
              <div class="flex flex-col min-w-0 py-0.5">
                <div class="flex items-center gap-1.5 min-w-0">
                  <Icon icon="lucide:map-pin" class="w-3.5 h-3.5 text-accent flex-shrink-0" />
                  <span class="text-[13px] font-medium text-main truncate">
                    {leaf}
                  </span>
                </div>
                <Show when={hierarchy.length > 0}>
                  <div class="flex items-center gap-1 text-[11px] text-dim truncate pl-5">
                    <span>{hierarchy.join(" › ")}</span>
                  </div>
                </Show>
              </div>
            );
          }}
        />

        {/* Collection */}
        <FilterCombobox
          label={t("home.collectionsLabel")}
          placeholder={t("home.filterCollection")}
          options={collections() || []}
          loading={collections.loading}
          value={props.selectedCollection}
          onChange={props.onCollectionChange}
          multiple={props.multiCollections}
          values={props.selectedCollections}
          onMultipleChange={props.onCollectionsChange}
          icon="lucide:archive"
          getOptionKey={(opt) => opt.key}
          getOptionLabel={(opt) => opt.label}
          getSelectedIcon={() => ({ icon: "lucide:archive", class: "text-accent" })}
          renderOption={(opt) => (
            <div class="flex items-center gap-2 min-w-0">
              <Icon icon="lucide:archive" class="w-3.5 h-3.5 text-accent flex-shrink-0" />
              <span class="text-[13px] font-medium text-main truncate" title={opt.label}>
                {opt.label}
              </span>
            </div>
          )}
        />

        {/* Record Type */}
        <FilterCombobox
          label={t("home.typesLabel")}
          placeholder={t("home.filterType")}
          options={types() || []}
          loading={types.loading}
          value={props.selectedType}
          onChange={props.onTypeChange}
          multiple={props.multiTypes}
          values={props.selectedTypes}
          onMultipleChange={props.onTypesChange}
          icon="lucide:tag"
          getOptionKey={(opt) => opt.key}
          getOptionLabel={(opt) => (t(`actCategories.${opt.key}` as any) || opt.label)}
          getSelectedIcon={(opt) => {
            const meta = ACT_CATEGORY_META[opt.key as ActTypeCategory];
            return meta ? { icon: meta.icon, class: "text-accent" } : { icon: "lucide:tag", class: "text-accent" };
          }}
          customFilter={(opt, query) => {
            const q = query.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim();
            if (!q) return true;
            const label = (t(`actCategories.${opt.key}` as any) || opt.label).toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
            const key = opt.key.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
            return label.includes(q) || key.includes(q);
          }}
          renderOption={(opt) => {
            const meta = ACT_CATEGORY_META[opt.key as ActTypeCategory] ?? {
              icon: "lucide:file-text",
              style: "text-event-other bg-event-other-bg border-event-other-border",
            };
            const label = t(`actCategories.${opt.key}` as any) || opt.label;
            return (
              <div class="flex items-center gap-2 min-w-0">
                <span class={["inline-flex items-center justify-center w-5 h-5 rounded border flex-shrink-0 text-xs", meta.style].join(" ")}>
                  <Icon icon={meta.icon} class="w-3 h-3" />
                </span>
                <span class="text-[13px] font-medium text-main truncate">
                  {label}
                </span>
              </div>
            );
          }}
        />

        {/* Date Range */}
        <div class="flex flex-col gap-1.5">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-1.5">
              <Icon icon="lucide:calendar" class="w-3.5 h-3.5 text-dim" />
              <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
                {t("home.datesLabel")}
              </label>
            </div>
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
          <div class="flex items-center rounded-lg border border-subtle bg-tinted overflow-hidden focus-within:border-accent focus-within:ring-2 focus-within:ring-accent/15 transition-all">
            <div class="relative flex-1">
              <input
                type="number"
                value={props.dateFrom}
                onInput={(e) => props.onDateFromChange(e.currentTarget.value)}
                placeholder={t("home.filterDateFrom")}
                class="w-full pl-3 pr-2 py-2 bg-transparent text-[13px] text-main placeholder:text-subtle-md outline-none no-spinner [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
            </div>
            <div class="text-subtle-md flex items-center px-1 pointer-events-none select-none">
              <Icon icon="lucide:arrow-right" class="w-3.5 h-3.5" />
            </div>
            <div class="relative flex-1">
              <input
                type="number"
                value={props.dateTo}
                onInput={(e) => props.onDateToChange(e.currentTarget.value)}
                placeholder={t("home.filterDateTo")}
                class="w-full pl-2 pr-3 py-2 bg-transparent text-[13px] text-main placeholder:text-subtle-md outline-none text-right no-spinner [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
            </div>
          </div>
        </div>
      </div>
    </aside>
  );
};

