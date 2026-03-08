import { For, createEffect } from "solid-js";
import { useI18n } from "../../ui/i18n";
import { createVirtualizer } from "@tanstack/solid-virtual";

interface ThumbnailBarProps {
  totalImages: number;
  currentImage: number;
  thumbnails?: Map<number, string>;
  onImageChange: (image: number) => void;
}

export const ThumbnailBar = (props: ThumbnailBarProps) => {
  let containerRef!: HTMLDivElement;
  const { t } = useI18n();

  const virtualizer = createVirtualizer({
    get count() { return props.totalImages; },
    getScrollElement: () => containerRef,
    estimateSize: () => 84, // 80px thumbnail width + 4px implicit gap
    horizontal: true,
    overscan: 5, // Keep 5 items rendered off-screen for smooth scrolling
    paddingStart: 12,
    paddingEnd: 12,
  });

  // Automatically scroll to the selected thumbnail
  createEffect(() => {
    virtualizer.scrollToIndex(props.currentImage - 1, { 
      align: "center", 
      behavior: "smooth" 
    });
  });

  return (
    <div
      ref={containerRef}
      role="listbox"
      aria-label={t("thumbnailBar.ariaLabel")}
      aria-orientation="horizontal"
      class={[
        "flex-shrink-0 h-[86px]",
        "bg-app border-t border-subtle overflow-x-auto",
        "scrollbar-thin scrollbar-thumb-subtle-md scrollbar-track-transparent",
      ].join(" ")}
    >
      {/* Fakes the total scrollable width */}
      <div 
        style={{ 
          width: `${virtualizer.getTotalSize()}px`, 
          height: '100%', 
          position: 'relative' 
        }}
      >
        <For each={virtualizer.getVirtualItems()}>
          {(virtualItem) => {
            const image = virtualItem.index + 1;
            const isActive = () => props.currentImage === image;
            const src = () => props.thumbnails?.get(image);

            return (
              <button
                type="button"
                role="option"
                aria-selected={isActive()}
                aria-label={t("thumbnailBar.imageLabel", { n: image })}
                data-image={image}
                onClick={() => props.onImageChange(image)}
                class={[
                  "flex flex-col items-center justify-center gap-1 rounded-md group",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
                ].join(" ")}
                style={{
                  // Position each item absolutely based on the virtualizer's calculations
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  height: '100%',
                  width: '80px', // Fixed thumbnail width
                  transform: `translateX(${virtualItem.start}px)`,
                }}
              >
                <div class={[
                  "w-20 h-[54px] rounded border-2 overflow-hidden transition-all duration-150",
                  "bg-panel flex items-center justify-center flex-shrink-0",
                  isActive()
                    ? "border-accent shadow-md shadow-accent/20"
                    : "border-subtle opacity-60 group-hover:opacity-100 group-hover:border-subtle-md group-hover:shadow-sm",
                ].join(" ")}>
                  {src()
                    ? <img src={src()} alt="" class="w-full h-full object-cover" loading="lazy" />
                    : <span class="text-[9px] text-dim font-mono leading-none">{image}</span>
                  }
                </div>
                <span class={[
                  "text-[10px] tabular-nums transition-colors",
                  isActive() ? "text-accent font-semibold" : "text-dim group-hover:text-muted",
                ].join(" ")}>
                  {image}
                </span>
              </button>
            );
          }}
        </For>
      </div>
    </div>
  );
};
