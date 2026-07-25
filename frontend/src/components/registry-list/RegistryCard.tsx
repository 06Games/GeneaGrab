import { ActTypeCategory, RegistryMeta } from "../../types/registry";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { Show } from "solid-js";
import { useTabs } from "../../contexts/TabsContext";

const ACT_TYPE_STYLES: Record<ActTypeCategory, string> = {
  vital: "text-event-vital bg-event-vital-bg border-event-vital-border",
  union: "text-event-union bg-event-union-bg border-event-union-border",
  mortality: "text-event-mortality bg-event-mortality-bg border-event-mortality-border",
  census: "text-event-census bg-event-census-bg border-event-census-border",
  legal: "text-event-legal bg-event-legal-bg border-event-legal-border",
  land: "text-event-land bg-event-land-bg border-event-land-border",
  media: "text-event-media bg-event-media-bg border-event-media-border",
  military: "text-event-military bg-event-military-bg border-event-military-border",
  other: "text-event-other bg-event-other-bg border-event-other-border",
  unknown: "text-event-other bg-event-other-bg border-event-other-border",
};

export const RegistryCard = (props: { registry: RegistryMeta; onContextMenu?: (e: MouseEvent) => void }) => {
  const { t } = useI18n();
  const { openTab } = useTabs();

  const getDomain = (url: string) => {
    try {
      return new URL(url).hostname;
    } catch {
      return url;
    }
  };

  return (
    <button
      onClick={() =>
        openTab({
          type: "registry",
          registryId: props.registry.id,
        })
      }
      onMouseDown={(e) => {
        if (e.button === 1) {
          e.preventDefault();
        }
      }}
      onAuxClick={(e) => {
        if (e.button === 1) {
          e.preventDefault();
          openTab(
            {
              type: "registry",
              registryId: props.registry.id,
            },
            false
          );
        }
      }}
      onContextMenu={(e) => {
        if (props.onContextMenu) {
          props.onContextMenu(e);
        }
      }}
      class="text-left flex flex-col h-[196px] bg-panel border border-subtle rounded-xl p-4 transition-all duration-150 hover:border-accent hover:shadow-md hover:shadow-accent/5 focus:outline-none focus:ring-2 focus:ring-accent group"
    >
      <div class="flex items-start justify-between mb-2">
        <h3 class="text-[15px] font-bold text-main truncate group-hover:text-accent transition-colors" title={props.registry.archive_reference}>
          {props.registry.archive_reference}
        </h3>
        <Show when={props.registry.date_from || props.registry.date_to}>
          <span class="text-[11px] font-medium text-dim bg-tinted px-2 py-0.5 rounded-md border border-subtle whitespace-nowrap ml-2 flex-shrink-0">
            {props.registry.date_from ? props.registry.date_from : "?"} – {props.registry.date_to ? props.registry.date_to : "?"}
          </span>
        </Show>
      </div>

      <div class="min-h-[20px] mb-2">
        <Show when={props.registry.title}>
          <p class="text-[13px] text-main truncate font-medium" title={props.registry.title}>
            {props.registry.title}
          </p>
        </Show>
      </div>

      <div class="flex flex-col gap-1.5 mb-3">
        <Show when={props.registry.places?.length > 0} fallback={<div class="h-[18px]" />}>
          <div class="flex items-center gap-1.5 text-[12px] text-muted truncate" title={props.registry.places.map((p) => p.join(", ")).join(" ; ")}>
            <Icon icon="lucide:map-pin" class="w-3.5 h-3.5 flex-shrink-0 text-dim" />
            <span class="truncate">{props.registry.places.map((p) => p[p.length - 1]).join(", ")}</span>
          </div>
        </Show>

        <Show when={props.registry.ark_url} fallback={<div class="h-[18px]" />}>
          <div class="flex items-center gap-1.5 text-[12px] text-muted truncate">
            <Icon icon="lucide:globe" class="w-3.5 h-3.5 flex-shrink-0 text-dim" />
            <span class="truncate">{getDomain(props.registry.ark_url || "")}</span>
          </div>
        </Show>
      </div>

      <div class="mt-auto pt-3 border-t border-subtle flex items-center justify-between gap-2">
        <div class="flex flex-nowrap overflow-hidden gap-1 min-w-0">
          {Array.from(props.registry.source_types).map((type) => (
            <span class={["text-[10px] font-medium px-1.5 py-0.5 rounded border truncate max-w-[120px] flex-shrink-0", ACT_TYPE_STYLES[type.category]].join(" ")}>
              {type.label as string}
            </span>
          ))}
        </div>
        <div class="flex items-center gap-2 text-[11px] text-dim flex-shrink-0 tabular-nums">
          <span class="flex items-center gap-1" title={t("home.imagesCount")}>
            <Icon icon="lucide:image" class="w-3 h-3" /> {props.registry.total_images}
          </span>
          <Show when={props.registry.acts_count !== undefined}>
            <span class="flex items-center gap-1" title={t("home.actsCount")}>
              <Icon icon="lucide:users" class="w-3 h-3" /> {props.registry.acts_count}
            </span>
          </Show>
        </div>
      </div>
    </button>
  );
};
