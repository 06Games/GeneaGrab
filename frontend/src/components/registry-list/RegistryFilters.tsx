import { createResource, Show } from "solid-js";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { useBackend } from "../../contexts/BackendContext";
import { ACT_TYPE_OPTIONS } from "../../types/registry";

interface RegistryFiltersProps {
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

  return (
    <div class="flex flex-col gap-3 bg-panel border border-subtle rounded-xl p-4 shadow-sm">
      <div class="relative w-full">
        <Icon icon="lucide:search" class="absolute left-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4" />
        <input
          type="text"
          value={props.searchQuery}
          onInput={(e) => props.onSearchChange(e.currentTarget.value)}
          placeholder={t("home.searchPlaceholder")}
          class="w-full pl-9 pr-4 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all"
        />
      </div>

      <div class="grid grid-cols-2 md:grid-cols-5 gap-3">
        {/* Place */}
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

        {/* Collection */}
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

        {/* Type */}
        <div class="relative">
          <select
            value={props.selectedType}
            onChange={(e) => props.onTypeChange(e.currentTarget.value)}
            class="w-full pl-3 pr-8 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all appearance-none cursor-pointer"
          >
            <option value="">{t("home.filterType")}</option>
            {ACT_TYPE_OPTIONS.map((opt) => (
              <option value={opt}>{opt}</option>
            ))}
          </select>
          <Icon icon="lucide:chevron-down" class="absolute right-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4 pointer-events-none" />
        </div>

        {/* Date From */}
        <div class="relative">
          <input
            type="number"
            value={props.dateFrom}
            onInput={(e) => props.onDateFromChange(e.currentTarget.value)}
            placeholder={t("home.filterDateFrom")}
            class="w-full px-3 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all"
          />
        </div>

        {/* Date To */}
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
  );
};
