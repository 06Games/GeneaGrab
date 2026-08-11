import { createSignal, createMemo, createEffect, Show, For, onCleanup, onMount, untrack, Switch, Match, createResource } from "solid-js";
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
import { isTauri } from "@tauri-apps/api/core";
import { getCurrent, onOpenUrl } from "@tauri-apps/plugin-deep-link";
import { openUrl } from "@tauri-apps/plugin-opener";

import { useTabs } from "../contexts/TabsContext";

const HomePage = () => {
  const api = useBackend();
  const { t } = useI18n();
  const { openTab } = useTabs();
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

  const [contextMenu, setContextMenu] = createSignal<{
    x: number;
    y: number;
    registry: RegistryMeta;
  } | null>(null);

  const handleGlobalClick = () => {
    setContextMenu(null);
  };

  const handleRegistryContextMenu = (e: MouseEvent, registry: RegistryMeta) => {
    e.preventDefault();
    e.stopPropagation();

    const menuWidth = 180;
    const menuHeight = 90;

    let x = e.clientX;
    let y = e.clientY;

    if (x + menuWidth > window.innerWidth) {
      x = window.innerWidth - menuWidth - 8;
    }
    if (y + menuHeight > window.innerHeight) {
      y = window.innerHeight - menuHeight - 8;
    }

    setContextMenu({
      x,
      y,
      registry,
    });
  };

  const handleOpenInBrowser = async (url: string) => {
    if (isTauri()) {
      try {
        await openUrl(url);
      } catch (err) {
        console.error("Failed to open URL via Tauri:", err);
        window.open(url, "_blank");
      }
    } else {
      window.open(url, "_blank");
    }
  };

  const handleDeleteRegistry = async (id: number) => {
    try {
      await api.deleteRegistry(id);
      setItems([]);
      fetchPage(null, true);
    } catch (err) {
      console.error("Failed to delete registry:", err);
    }
  };

  const isUrl = createMemo(() => {
    const q = searchQuery().trim();
    try {
      new URL(q);
      return true;
    } catch {
      return false;
    }
  });

  const [providers] = createResource(
    () => (isUrl() ? searchQuery().trim() : null),
    (url) => api.getProvidersForUrl(url),
  );

  const [selectedQuickProvider, setSelectedQuickProvider] = createSignal("");
  const [isAdding, setIsAdding] = createSignal(false);

  createEffect(() => {
    const list = providers();
    if (list && list.length > 0) {
      setSelectedQuickProvider(list[0].id);
    } else {
      setSelectedQuickProvider("");
    }
  });

  const isAlreadyAdded = createMemo(() => {
    const q = searchQuery().trim();
    if (!q) return false;
    const currentProviders = providers();
    if (isUrl() && currentProviders && currentProviders.length > 0) {
      return items().some((r) =>
        currentProviders.some(
          (p) => r.source_id === p.id && (!p.registry_id || r.registry_id === p.registry_id)
        )
      );
    }
    return false;
  });

  const extractedImageNumber = createMemo(() => {
    if (!isUrl()) return undefined;
    const currentProviders = providers();
    if (!currentProviders || currentProviders.length === 0) return undefined;
    return currentProviders[0].image_number;
  });

  const handleOpenExtractedUrl = () => {
    const currentProviders = providers();
    if (!currentProviders || currentProviders.length === 0) return;
    const matchingProvider = currentProviders[0];
    const reg = items().find(
      (r) => r.source_id === matchingProvider.id && (!matchingProvider.registry_id || r.registry_id === matchingProvider.registry_id)
    );
    if (reg) {
      openTab({
        type: "registry",
        registryId: reg.id,
        imageId: matchingProvider.image_number || 1,
      });
    }
  };

  const handleQuickAdd = async () => {
    if (!searchQuery() || !selectedQuickProvider() || isAdding()) return;
    setIsAdding(true);
    try {
      const newRegistry = await api.addRegistry(searchQuery().trim(), selectedQuickProvider());
      // Refresh list to instantly show the new registry
      setItems([]);
      fetchPage(null, true);

      const currentProviders = providers();
      const selectedP = currentProviders?.find((p) => p.id === selectedQuickProvider());
      openTab({
        type: "registry",
        registryId: newRegistry.id,
        imageId: selectedP?.image_number || 1,
      });
    } catch (err) {
      console.error(err);
    } finally {
      setIsAdding(false);
    }
  };

  function getSearchUrlFromScheme(urlStr: string): string | null {
    try {
      const uri = new URL(urlStr);
      const paramUrl = uri.searchParams.get("url");
      if (paramUrl) return paramUrl;
    } catch {}

    const match = urlStr.match(/[?&]url=([^&]+)/);
    if (match) {
      try {
        return decodeURIComponent(match[1]);
      } catch {
        return match[1];
      }
    }
    return null;
  }

  async function handleCustomScheme(urlStr: string) {
    console.log("Opened with custom scheme:", urlStr);
    const search_url = getSearchUrlFromScheme(urlStr);
    if (!search_url) return;

    // 1. Switch to Home tab automatically
    openTab({ type: "home" });

    // 2. Set search query in Home page search bar
    setSearchQuery(search_url);

    // 3. Query provider identification and database registries directly
    try {
      const [providerList, registryRes] = await Promise.all([
        api.getProvidersForUrl(search_url).catch(() => []),
        api.getAllRegistries({
          limit: 20,
          cursor: null,
          filters: { search_term: search_url },
        }).catch(() => ({ data: [] })),
      ]);

      if (providerList && providerList.length > 0 && registryRes.data && registryRes.data.length > 0) {
        const matchingProvider = providerList[0];
        const matchedRegistry = registryRes.data.find(
          (r) =>
            r.source_id === matchingProvider.id &&
            (!matchingProvider.registry_id || r.registry_id === matchingProvider.registry_id)
        );

        if (matchedRegistry) {
          // Open or switch to the matching registry at the right page!
          openTab({
            type: "registry",
            registryId: matchedRegistry.id,
            imageId: matchingProvider.image_number || 1,
          });
        }
      }
    } catch (err) {
      console.error("Error handling deep link custom scheme:", err);
    }
  }

  onMount(async () => {
    const initialUrl: any = null; // FIXEME
    if (initialUrl) {
      setSearchQuery(decodeURIComponent(initialUrl instanceof Array ? initialUrl[0] : initialUrl));
    }
    if (isTauri()) {
      (await getCurrent())?.forEach(handleCustomScheme);
      await onOpenUrl((urls) => urls.forEach(handleCustomScheme));
    }

    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const containerWidth = entry.contentRect.width;
        // 280px minimum width + 16px gap = 296px space per column
        const cols = Math.max(1, Math.floor((containerWidth + 16) / 296));
        setColumns(cols);
      }
    });

    if (scrollRef) observer.observe(scrollRef);

    window.addEventListener("click", handleGlobalClick);
    window.addEventListener("contextmenu", handleGlobalClick);

    onCleanup(() => {
      observer.disconnect();
      window.removeEventListener("click", handleGlobalClick);
      window.removeEventListener("contextmenu", handleGlobalClick);
    });
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

  let lastFetchId = 0;
  const fetchPage = async (cursor: number | null, isInitial: boolean) => {
    if (isFetching() && !isInitial) return;

    const currentFetchId = ++lastFetchId;
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

      // If a newer fetch has started since this one, ignore these results
      if (currentFetchId !== lastFetchId) return;

      if (isInitial) setItems(res.data);
      else setItems((prev) => [...prev, ...res.data]);

      setNextCursor(res.next_cursor);
      setHasNextPage(res.next_cursor !== null);
    } catch (err) {
      if (currentFetchId === lastFetchId) {
        console.error("Failed to fetch registries:", err);
      }
    } finally {
      if (currentFetchId === lastFetchId) {
        setIsFetching(false);
      }
    }
  };

  createEffect((prevDeps) => {
    const currentDeps = [searchQuery(), selectedType(), selectedPlace(), selectedCollection(), dateFrom(), dateTo()].join("|");
    if (prevDeps !== currentDeps) {
      setItems([]);
      untrack(() => fetchPage(null, true));
    }
    return currentDeps;
  }, "");

  const virtualizer = createVirtualizer({
    get count() {
      return hasNextPage() ? chunkedRows().length + 1 : chunkedRows().length;
    },
    getScrollElement: () => scrollRef,
    estimateSize: () => 196,
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
    <div class="w-full h-full bg-app text-main flex flex-col antialiased" style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}>
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

        <Show when={isUrl() && !isFetching()}>
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 -mb-2 bg-panel border border-accent rounded-xl shadow-sm animate-in fade-in slide-in-from-top-2">
            <div class="flex items-start gap-3">
              <div class="mt-0.5 text-accent">
                <Icon icon="lucide:link" width="20" height="20" />
              </div>
              <div class="flex flex-col">
                <span class="text-[14px] font-semibold text-main">{t("home.urlDetected")}</span>
                <span class="text-[13px] text-dim">
                  <Switch>
                    <Match when={isAlreadyAdded()}>{t("home.urlAlreadyAdded")}</Match>
                    <Match when={providers.loading}>{t("home.checkingUrl")}</Match>
                    <Match when={!providers.loading && providers()?.length === 0}>
                      <span class="text-warning">{t("home.urlNoProvider")}</span>
                    </Match>
                    <Match when={!providers.loading && providers() && providers()!.length > 0}>{t("home.urlReady")}</Match>
                  </Switch>
                </span>
              </div>
            </div>

            <Show when={isAlreadyAdded()}>
              <div class="flex items-center gap-2">
                <Button variant="primary" onClick={handleOpenExtractedUrl}>
                  <Icon icon="lucide:external-link" /> {t("home.open")}
                </Button>
              </div>
            </Show>

            <Show when={!isAlreadyAdded() && !providers.loading && providers() && providers()!.length > 0}>
              <div class="flex items-center gap-2">
                <select
                  value={selectedQuickProvider()}
                  onChange={(e) => setSelectedQuickProvider(e.currentTarget.value)}
                  class="px-3 py-1.5 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent outline-none appearance-none cursor-pointer"
                >
                  <For each={providers()}>{(p) => <option value={p.id}>{p.name}</option>}</For>
                </select>
                <Button variant="primary" onClick={handleQuickAdd} disabled={isAdding()}>
                  <Show
                    when={isAdding()}
                    fallback={
                      <>
                        <Icon icon="lucide:plus" /> {t("home.addQuick")}
                      </>
                    }
                  >
                    <Icon icon="lucide:loader-2" class="animate-spin" />
                  </Show>
                </Button>
              </div>
            </Show>
          </div>
        </Show>

        <div
          ref={scrollRef}
          class="flex-1 overflow-y-auto -mr-4 pr-4 pb-4 min-h-0 scrollbar-thin scrollbar-thumb-subtle hover:scrollbar-thumb-subtle-md focus-visible:outline-none"
        >
          <Show when={!isFetching() && items().length === 0}>
            <div class="py-16 flex flex-col items-center justify-center text-dim border-2 border-dashed border-subtle rounded-xl h-full">
              <Icon icon="lucide:folder-search" class="w-12 h-12 mb-3 text-subtle-md" />
              <p>{t("home.noResults")}</p>
            </div>
          </Show>

          <div style={{ height: `${virtualizer.getTotalSize()}px`, width: "100%", position: "relative" }}>
            <For each={virtualizer.getVirtualItems()}>
              {(virtualRow) => (
                <div
                  ref={(el) => {
                    createEffect(() => {
                      const idx = virtualRow.index;
                      if (el) {
                        virtualizer.measureElement(el);
                      }
                    });
                  }}
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
                      <div class="w-full flex justify-center items-center h-[196px] text-dim">
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
                      <For each={chunkedRows()[virtualRow.index]}>{(item) => <RegistryCard registry={item} targetImageId={extractedImageNumber()} onContextMenu={(e) => handleRegistryContextMenu(e, item)} />}</For>
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

      <Show when={contextMenu()}>
        {(menu) => (
          <div
            class="fixed z-50 bg-panel border border-subtle rounded-lg shadow-xl py-1 min-w-[180px] transition-all duration-100"
            style={{
              left: `${menu().x}px`,
              top: `${menu().y}px`,
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <button
              onClick={() => {
                const url = menu().registry.ark_url;
                if (url) {
                  handleOpenInBrowser(url);
                }
                setContextMenu(null);
              }}
              disabled={!menu().registry.ark_url}
              class="w-full text-left px-3 py-2 text-[13px] text-main hover:bg-hover disabled:opacity-40 disabled:hover:bg-transparent flex items-center gap-2 transition-colors cursor-pointer disabled:cursor-not-allowed font-medium"
            >
              <Icon icon="lucide:external-link" class="w-4 h-4 text-muted" />
              {t("home.openInBrowser")}
            </button>
            <div class="h-px bg-subtle my-1" />
            <button
              onClick={() => {
                if (window.confirm(t("home.confirmDelete"))) {
                  handleDeleteRegistry(menu().registry.id);
                }
                setContextMenu(null);
              }}
              class="w-full text-left px-3 py-2 text-[13px] text-danger hover:bg-danger/10 flex items-center gap-2 transition-colors cursor-pointer font-medium"
            >
              <Icon icon="lucide:trash-2" class="w-4 h-4" />
              {t("home.delete")}
            </button>
          </div>
        )}
      </Show>
    </div>
  );
};

export default HomePage;
