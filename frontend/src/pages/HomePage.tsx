import { createSignal, createMemo, createEffect, Show, For, onCleanup, onMount, untrack } from "solid-js";
import { createVirtualizer } from "@tanstack/solid-virtual";
import { useBackend } from "../contexts/BackendContext";
import { useI18n } from "../ui/i18n";
import { RegistryCard } from "../components/registry-list/RegistryCard";
import { RegistryFilters } from "../components/registry-list/RegistryFilters";
import { AddRegistryModal } from "../components/registry-list/AddRegistryModal";
import { TopBar } from "../ui/TopBar";
import { Button } from "../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import type { RegistryMeta } from "../types/registry";

const HomePage = () => {
  const api = useBackend();
  const { t } = useI18n();
  let scrollRef!: HTMLDivElement;

  const [searchQuery, setSearchQuery] = createSignal("");
  const [selectedType, setSelectedType] = createSignal("");
  const [selectedPlace, setSelectedPlace] = createSignal("");
  const [selectedCollection, setSelectedCollection] = createSignal("");
  const [dateFrom, setDateFrom] = createSignal("");
  const [dateTo, setDateTo] = createSignal("");

  const [isModalOpen, setIsModalOpen] = createSignal(false);

  // Data / Pagination state
  const [items, setItems] = createSignal<RegistryMeta[]>([]);
  const [nextCursor, setNextCursor] = createSignal<number | null>(null);
  const [hasNextPage, setHasNextPage] = createSignal(true);
  const [isFetching, setIsFetching] = createSignal(false);

  const [columns, setColumns] = createSignal(1);

  onMount(() => {
    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const containerWidth = entry.contentRect.width;
        // 280px minimum width + 16px gap = 296px space per column
        const cols = Math.max(1, Math.floor((containerWidth + 16) / 296));
        setColumns(cols);
      }
    });

    if (scrollRef) observer.observe(scrollRef);
    onCleanup(() => observer.disconnect());
  });

  const chunkedRows = createMemo(() => {
    const res: RegistryMeta[][] = [];
    const currentItems = items();
    const cols = columns();
    for (let i = 0; i < currentItems.length; i += cols) {
      res.push(currentItems.slice(i, i + cols));
    }
    return res;
  });

  const fetchPage = async (cursor: number | null, isInitial: boolean) => {
    if (isFetching()) return;
    setIsFetching(true);

    try {
      const payload = {
        limit: 20,
        cursor: cursor,
        filters: {
          search_term: searchQuery(),
          source_type: selectedType(),
          place: selectedPlace(),
          collection: selectedCollection(),
          date_from: dateFrom(),
          date_to: dateTo(),
        },
      };

      const res = await api.getAllRegistries(payload);

      if (isInitial) setItems(res.data);
      else setItems((prev) => [...prev, ...res.data]);

      setNextCursor(res.next_cursor);
      setHasNextPage(res.next_cursor !== null);
    } catch (err) {
      console.error("Failed to fetch registries:", err);
    } finally {
      setIsFetching(false);
    }
  };

  createEffect((prevDeps) => {
    const currentDeps = [searchQuery(), selectedType(), selectedPlace(), selectedCollection(), dateFrom(), dateTo()].join("|");
    if (prevDeps !== currentDeps) {
      setItems([]);
      fetchPage(null, true);
    }
    return currentDeps;
  }, "");

  const virtualizer = createVirtualizer({
    get count() {
      return hasNextPage() ? chunkedRows().length + 1 : chunkedRows().length;
    },
    getScrollElement: () => scrollRef,
    estimateSize: () => 188,
    gap: 16,
    overscan: 4,
  });

  createEffect(() => {
    const virtualItems = virtualizer.getVirtualItems();
    if (!virtualItems.length) return;

    const lastRenderedItem = virtualItems[virtualItems.length - 1];

    if (lastRenderedItem.index >= chunkedRows().length - 1 && hasNextPage() && !isFetching()) {
      fetchPage(untrack(nextCursor), false);
    }
  });

  return (
    <div class="h-screen bg-app text-main flex flex-col antialiased" style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}>
      <TopBar
        breadcrumbs={[t("home.title")]}
        right={
          <Button variant="primary" onClick={() => setIsModalOpen(true)}>
            <Icon icon="lucide:plus" class="w-4 h-4" /> {t("home.addRegistry")}
          </Button>
        }
      />

      <main class="flex-1 max-w-6xl w-full mx-auto px-6 pt-8 pb-4 flex flex-col gap-6 min-h-0">
        <RegistryFilters
          searchQuery={searchQuery()}
          onSearchChange={setSearchQuery}
          selectedType={selectedType()}
          onTypeChange={setSelectedType}
          selectedPlace={selectedPlace()}
          onPlaceChange={setSelectedPlace}
          selectedCollection={selectedCollection()}
          onCollectionChange={setSelectedCollection}
          dateFrom={dateFrom()}
          onDateFromChange={setDateFrom}
          dateTo={dateTo()}
          onDateToChange={setDateTo}
        />

        <div
          ref={scrollRef}
          class="flex-1 overflow-y-auto pr-2 min-h-0 scrollbar-thin scrollbar-thumb-subtle hover:scrollbar-thumb-subtle-md focus-visible:outline-none"
        >
          <Show when={!isFetching() && items().length === 0}>
            <div class="py-16 flex flex-col items-center justify-center text-dim border-2 border-dashed border-subtle rounded-xl h-full">
              <Icon icon="lucide:folder-search" class="w-12 h-12 mb-3 text-subtle-md" />
              <p>{t("home.noResults")}</p>
            </div>
          </Show>

          {/* TODO: Card display isn't great to use and needs to be ordered */}
          <div style={{ height: `${virtualizer.getTotalSize()}px`, width: "100%", position: "relative" }}>
            <For each={virtualizer.getVirtualItems()}>
              {(virtualRow) => (
                <div
                  ref={virtualizer.measureElement}
                  data-index={virtualRow.index}
                  style={{
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                >
                  <Show
                    when={virtualRow.index < chunkedRows().length}
                    fallback={
                      <div class="w-full flex justify-center items-center h-[188px] text-dim">
                        <Icon icon="lucide:loader-2" class="animate-spin" width="24" height="24" />
                      </div>
                    }
                  >
                    <div
                      class="grid"
                      style={{
                        "grid-template-columns": `repeat(${columns()}, minmax(0, 1fr))`,
                        gap: "16px",
                      }}
                    >
                      <For each={chunkedRows()[virtualRow.index]}>{(item) => <RegistryCard registry={item} />}</For>
                    </div>
                  </Show>
                </div>
              )}
            </For>
          </div>
        </div>
      </main>

      <Show when={isModalOpen()}>
        <AddRegistryModal
          onClose={() => setIsModalOpen(false)}
          onAdded={() => {
            setIsModalOpen(false);
            fetchPage(null, true);
          }}
        />
      </Show>
    </div>
  );
};

export default HomePage;
