import { RegistryMeta } from "../../types/registry";
import { Icon } from "@iconify-icon/solid";
import { Badge } from "../../ui/primitives";
import { useI18n } from "../../ui/i18n";

export const RegistryCard = (props: { registry: RegistryMeta }) => {
  const { t } = useI18n();

  return (
    <a
      href={`/registry/${props.registry.registry_id}`}
      class="flex flex-col bg-panel border border-subtle rounded-xl p-4 transition-all duration-150 hover:border-accent hover:shadow-md hover:shadow-accent/5 focus:outline-none focus:ring-2 focus:ring-accent"
    >
      <div class="flex items-start justify-between mb-3">
        <div class="flex items-center justify-center w-10 h-10 rounded-lg bg-tinted border border-subtle text-dim">
          <Icon icon="lucide:book-open" class="w-5 h-5" />
        </div>
        <span class="text-[11px] font-medium text-dim bg-app px-2 py-0.5 rounded-full border border-subtle">
          {props.registry.total_images} vues
        </span>
      </div>

      <h3 class="text-[15px] font-bold text-main truncate mb-1" title={props.registry.archive_reference}>
        {props.registry.archive_reference}
      </h3>

      <div class="flex items-center gap-1.5 text-[13px] text-muted mb-4 truncate">
        <Icon icon="lucide:map-pin" class="w-3.5 h-3.5 flex-shrink-0" />
        <span class="truncate">{props.registry.town}</span>
      </div>

      <div class="mt-auto flex flex-wrap gap-1.5">
        {Array.from(props.registry.source_types).map(type => (
           <Badge class="bg-app border-subtle text-dim">{type as string}</Badge>
        ))}
      </div>
    </a>
  );
}
