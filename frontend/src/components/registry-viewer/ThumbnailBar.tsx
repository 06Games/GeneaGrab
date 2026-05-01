import { For, createEffect, createSignal } from "solid-js";
import { useI18n } from "../../ui/i18n";
import { createVirtualizer } from "@tanstack/solid-virtual";
import { getBackendService } from "../../services/apiFactory";
import { UserImageMeta } from "../../types/image";

const THUMBNAIL_ASPECT_RATIO = 1.3;
const THUMBNAIL_VERTICAL_PADDING = 16; // Combined height for the image number label and gap below the thumbnail

interface ThumbnailBarProps {
  totalImages: number;
  currentImage: number;
  images: UserImageMeta[];
  registryId: number;
  height: number;
  onImageChange: (image: number) => void;
}

export const ThumbnailBar = (props: ThumbnailBarProps) => {
  let containerRef!: HTMLDivElement;
  const { t } = useI18n();
  const api = getBackendService();

  const [failedImages, setFailedImages] = createSignal<Set<number>>(new Set());

  const thumbnailWidth = () => (props.height - THUMBNAIL_VERTICAL_PADDING) * THUMBNAIL_ASPECT_RATIO;

  const virtualizer = createVirtualizer({
    get count() {
      return props.totalImages;
    },
    getScrollElement: () => containerRef,
    estimateSize: () => thumbnailWidth() + 4, // width + 4px implicit gap
    horizontal: true,
    overscan: 5, // Keep 5 items rendered off-screen for smooth scrolling
    paddingStart: 12,
    paddingEnd: 12,
  });

  // Automatically scroll to the selected thumbnail
  createEffect(() => {
    virtualizer.scrollToIndex(props.currentImage - 1, {
      align: "center",
      behavior: "smooth",
    });
  });

  return (
    <div
      ref={containerRef}
      role="listbox"
      aria-label={t("thumbnailBar.ariaLabel")}
      aria-orientation="horizontal"
      style={{ height: `${props.height}px` }}
      class={["flex-shrink-0", "bg-app border-t border-subtle overflow-x-auto", "scrollbar-thin scrollbar-thumb-subtle-md scrollbar-track-transparent"].join(
        " ",
      )}
    >
      {/* Fakes the total scrollable width */}
      <div
        style={{
          width: `${virtualizer.getTotalSize()}px`,
          height: "100%",
          position: "relative",
        }}
      >
        <For each={virtualizer.getVirtualItems()}>
          {(virtualItem) => {
            const image_number = virtualItem.index + 1;
            const image: UserImageMeta | undefined = props.images[virtualItem.index];
            const isActive = () => props.currentImage === image_number;
            const src = () => api.getTileUrl(props.registryId, image_number, 0, 0, 0);
            const hasError = () => failedImages().has(image_number);

            // TODO: Display some image metadata (like time period)
            return (
              <button
                type="button"
                role="option"
                aria-selected={isActive()}
                aria-label={t("thumbnailBar.imageLabel", { n: image_number })}
                data-image={image_number}
                onClick={() => props.onImageChange(image_number)}
                class={[
                  "h-full flex flex-col items-center justify-center gap-1 rounded-md group",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
                ].join(" ")}
                style={{
                  // Position each item absolutely based on the virtualizer's calculations
                  position: "absolute",
                  top: 0,
                  left: 0,
                  height: "100%",
                  width: `${thumbnailWidth()}px`,
                  transform: `translateX(${virtualItem.start}px)`,
                }}
              >
                <div
                  class={[
                    "w-full rounded border-2 overflow-hidden transition-all duration-150",
                    "bg-panel flex items-center justify-center flex-1",
                    isActive()
                      ? "border-accent shadow-md shadow-accent/20"
                      : "border-subtle opacity-60 group-hover:opacity-100 group-hover:border-subtle-md group-hover:shadow-sm",
                  ].join(" ")}
                >
                  {src() && !hasError() ? (
                    <img
                      src={src()!}
                      alt=""
                      class="w-full h-full object-contain"
                      loading="lazy"
                      onError={() => {
                        const next = new Set(failedImages());
                        next.add(image_number);
                        setFailedImages(next);
                      }}
                    />
                  ) : (
                    <span class="text-[9px] text-dim font-mono leading-none">{image_number}</span>
                  )}
                </div>
                <span
                  class={[
                    "text-[10px] tabular-nums transition-colors flex-shrink-0",
                    isActive() ? "text-accent font-semibold" : "text-dim group-hover:text-muted",
                  ].join(" ")}
                >
                  {image?.name ? `${image.name} (${image_number})` : image_number}
                </span>
              </button>
            );
          }}
        </For>
      </div>
    </div>
  );
};
