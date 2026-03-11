import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { EVENT_TYPE_OPTIONS } from "../../types/registry";

interface RegistryFiltersProps {
  searchQuery: string;
  onSearchChange: (val: string) => void;
  selectedType: string;
  onTypeChange: (val: string) => void;
}

export const RegistryFilters = (props: RegistryFiltersProps) => {
  const { t } = useI18n();

  return (
    <div class="flex flex-col sm:flex-row items-center gap-3">
      <div class="relative flex-1 w-full">
        <Icon icon="lucide:search" class="absolute left-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4" />
        <input
          type="text"
          value={props.searchQuery}
          onInput={(e) => props.onSearchChange(e.currentTarget.value)}
          placeholder={t("home.searchPlaceholder")}
          class="w-full pl-9 pr-4 py-2 rounded-lg border border-subtle bg-panel text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all"
        />
      </div>

      <div class="w-full sm:w-48 flex-shrink-0 relative">
        <select
          value={props.selectedType}
          onChange={(e) => props.onTypeChange(e.currentTarget.value)}
          class="w-full pl-3 pr-8 py-2 rounded-lg border border-subtle bg-panel text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/15 outline-none transition-all appearance-none cursor-pointer"
        >
          <option value="">{t("home.filterType")}</option>
          {EVENT_TYPE_OPTIONS.map(opt => (
            <option value={opt}>{opt}</option>
          ))}
        </select>
        <Icon icon="lucide:chevron-down" class="absolute right-3 top-1/2 -translate-y-1/2 text-subtle-md w-4 h-4 pointer-events-none" />
      </div>
    </div>
  );
}
