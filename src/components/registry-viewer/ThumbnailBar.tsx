import { For, createEffect } from "solid-js";

interface ThumbnailBarProps {
  totalPages: number;
  currentPage: number;
  thumbnails?: Map<number, string>;
  onPageChange: (page: number) => void;
}

/**
 * ThumbnailBar — horizontal folio navigation strip.
 * Production: replace <For> with @tanstack/solid-virtual.
 */
export const ThumbnailBar = (props: ThumbnailBarProps) => {
  let containerRef!: HTMLDivElement;

  const pages = () => Array.from({ length: props.totalPages }, (_, i) => i + 1);

  createEffect(() => {
    const active = props.currentPage;
    const el = containerRef?.querySelector<HTMLButtonElement>(`[data-page="${active}"]`);
    el?.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
  });

  return (
    <div
      ref={containerRef!}
      role="listbox"
      aria-label="Miniatures des pages"
      aria-orientation="horizontal"
      class={[
        "flex-shrink-0 h-[86px] flex items-center gap-1 px-3",
        "bg-[#f7f3ee] border-t border-[#e0d8cc] overflow-x-auto",
        "scrollbar-thin scrollbar-thumb-[#ccc4b8] scrollbar-track-transparent",
      ].join(" ")}
    >
      <For each={pages()}>
        {(page) => {
          const isActive = () => props.currentPage === page;
          const src = () => props.thumbnails?.get(page);
          return (
            <button
              type="button"
              role="option"
              aria-selected={isActive()}
              aria-label={`Page ${page}`}
              data-page={page}
              onClick={() => props.onPageChange(page)}
              class={[
                "flex-shrink-0 flex flex-col items-center gap-1 rounded-md group",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#b8743a]",
              ].join(" ")}
            >
              <div class={[
                "w-10 h-[54px] rounded border-2 overflow-hidden transition-all duration-150",
                "bg-white flex items-center justify-center",
                isActive()
                  ? "border-[#b8743a] shadow-md shadow-[#b8743a]/20"
                  : "border-[#e0d8cc] opacity-60 group-hover:opacity-100 group-hover:border-[#ccc4b8] group-hover:shadow-sm",
              ].join(" ")}>
                {src()
                  ? <img src={src()} alt="" class="w-full h-full object-cover" loading="lazy" />
                  : <span class="text-[9px] text-[#a89e93] font-mono leading-none">{page}</span>
                }
              </div>
              <span class={[
                "text-[10px] tabular-nums transition-colors",
                isActive() ? "text-[#b8743a] font-semibold" : "text-[#a89e93] group-hover:text-[#6b6358]",
              ].join(" ")}>
                {page}
              </span>
            </button>
          );
        }}
      </For>
    </div>
  );
};
