import { For, createEffect } from "solid-js";

interface ThumbnailBarProps {
  totalImages: number;
  currentImage: number;
  thumbnails?: Map<number, string>;
  onImageChange: (image: number) => void;
}

/**
 * ThumbnailBar — horizontal folio navigation strip.
 * Production: replace <For> with @tanstack/solid-virtual.
 */
export const ThumbnailBar = (props: ThumbnailBarProps) => {
  let containerRef!: HTMLDivElement;

  const images = () => Array.from({ length: props.totalImages }, (_, i) => i + 1);

  createEffect(() => {
    const active = props.currentImage;
    const el = containerRef?.querySelector<HTMLButtonElement>(`[data-image="${active}"]`);
    el?.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
  });

  return (
    <div
      ref={containerRef!}
      role="listbox"
      aria-label="Miniatures des prises de vue"
      aria-orientation="horizontal"
      class={[
        "flex-shrink-0 h-[86px] flex items-center gap-1 px-3",
        "bg-app border-t border-subtle overflow-x-auto",
        "scrollbar-thin scrollbar-thumb-subtle-md scrollbar-track-transparent",
      ].join(" ")}
    >
      <For each={images()}>
        {(image) => {
          const isActive = () => props.currentImage === image;
          const src = () => props.thumbnails?.get(image);
          return (
            <button
              type="button"
              role="option"
              aria-selected={isActive()}
              aria-label={`Image ${image}`}
              data-image={image}
              onClick={() => props.onImageChange(image)}
              class={[
                "flex-shrink-0 flex flex-col items-center gap-1 rounded-md group",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
              ].join(" ")}
            >
              <div class={[
                "w-20 h-[54px] rounded border-2 overflow-hidden transition-all duration-150",
                "bg-panel flex items-center justify-center",
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
  );
};
