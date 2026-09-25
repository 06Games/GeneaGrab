import { createSignal, createMemo, createEffect, Show, For, onCleanup, onMount, untrack, Switch, Match, createResource } from "solid-js";
import { createVirtualizer } from "@tanstack/solid-virtual";
import { useBackend } from "../contexts/BackendContext";
import { useI18n } from "../ui/i18n";
import { RegistryCard } from "../components/registry-list/RegistryCard";
import { RegistryFilters } from "../components/registry-list/RegistryFilters";
import { AddRegistryModal } from "../components/registry-list/AddRegistryModal";
import { TopBar } from "../ui/TopBar";
import { Button, ResizeHandle } from "../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import type { RegistryMeta } from "../types/registry";
import { isTauri } from "@tauri-apps/api/core";
import { getCurrent, onOpenUrl } from "@tauri-apps/plugin-deep-link";
import { openUrl } from "@tauri-apps/plugin-opener";

import { useTabs } from "../contexts/TabsContext";

interface LocationGroup {
  key: string;
  label: string;
  parts: string[];
  registries: RegistryMeta[];
}

function parsePlaceParts(rawPlace: any): string[] {
  if (Array.isArray(rawPlace)) {
    const res: string[] = [];
    for (const item of rawPlace) {
      if (typeof item === "string" && item.includes(",")) {
        res.push(...item.split(",").map((s) => s.trim()).filter(Boolean));
      } else if (item) {
        res.push(String(item).trim());
      }
    }
    return res.filter(Boolean);
  }
  if (typeof rawPlace === "string") {
    return rawPlace.split(",").map((s) => s.trim()).filter(Boolean);
  }
  return [];
}

type VirtualRowItem =
  | {
      type: "header";
      key: string;
      groupKey: string;
      label: string;
      parts: string[];
      count: number;
      isCollapsed: boolean;
    }
  | {
      type: "cards";
      key: string;
      groupKey: string;
      items: RegistryMeta[];
    };

const LocationGroupHeader = (props: {
  label: string;
  parts: string[];
  count: number;
  isCollapsed: boolean;
  onToggle: () => void;
}) => (
  <button
    type="button"
    onClick={props.onToggle}
    title={props.label}
    class="w-full flex items-center justify-between py-2.5 px-3 rounded-lg bg-panel hover:bg-hover border border-subtle text-left group/header select-none focus:outline-none focus:ring-1 focus:ring-accent cursor-pointer transition-colors duration-100"
  >
    <div class="flex items-center gap-2 min-w-0 flex-1">
      <Icon
        icon="lucide:chevron-right"
        class={[
          "w-4 h-4 text-dim group-hover/header:text-main transition-transform duration-150 flex-shrink-0",
          !props.isCollapsed ? "rotate-90" : "",
        ].join(" ")}
      />
      <Icon icon="lucide:map-pin" class="w-4 h-4 text-accent flex-shrink-0" />
      <div class="flex items-center gap-1.5 flex-wrap min-w-0 text-[13px]">
        <For each={props.parts}>
          {(part, index) => (
            <>
              <Show when={index() > 0}>
                <span class="text-subtle-md text-[11px] font-bold select-none">&gt;</span>
              </Show>
              <span
                class={[
                  "truncate",
                  index() === props.parts.length - 1
                    ? "font-bold text-main group-hover/header:text-accent transition-colors"
                    : "text-muted font-medium",
                ].join(" ")}
              >
                {part}
              </span>
            </>
          )}
        </For>
      </div>
    </div>
    <span class="ml-3 px-2 py-0.5 rounded-full bg-tinted border border-subtle text-dim text-[11px] tabular-nums font-semibold leading-none flex-shrink-0">
      {props.count}
    </span>
  </button>
);

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

  const DEFAULT_SIDEBAR_WIDTH = 280;
  const MIN_SIDEBAR_WIDTH = 220;
  const MAX_SIDEBAR_WIDTH = 480;

  const [sidebarWidth, setSidebarWidth] = createSignal(DEFAULT_SIDEBAR_WIDTH);

  const onSidebarResizePointerDown = (e: PointerEvent) => {
    e.preventDefault();
    const startX = e.clientX;
    const startW = sidebarWidth();
    const onMove = (ev: PointerEvent) => {
      setSidebarWidth(Math.max(MIN_SIDEBAR_WIDTH, Math.min(MAX_SIDEBAR_WIDTH, startW + (ev.clientX - startX))));
    };
    const onUp = () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  };

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

  const [expandedGroups, setExpandedGroups] = createSignal<Record<string, boolean>>({});

  const toggleGroup = (key: string) => {
    setExpandedGroups((prev) => ({
      ...prev,
      [key]: !prev[key],
    }));
  };

  const locationGroups = createMemo(() => {
    const currentItems = items();
    const unknownLabel = t("home.unknownLocation");
    const groupMap = new Map<string, LocationGroup>();

    for (const item of currentItems) {
      const rawPlaces = item.places && Array.isArray(item.places) ? item.places : [];
      const validPlaces: { key: string; label: string; parts: string[] }[] = [];
      const seenKeysInItem = new Set<string>();

      for (const p of rawPlaces) {
        const parsedParts = parsePlaceParts(p);
        if (parsedParts.length > 0) {
          const label = parsedParts.join(" > ");
          const key = label.toLowerCase();
          if (!seenKeysInItem.has(key)) {
            seenKeysInItem.add(key);
            validPlaces.push({ key, label, parts: parsedParts });
          }
        }
      }

      if (validPlaces.length === 0) {
        const unknownKey = "__unknown__";
        let group = groupMap.get(unknownKey);
        if (!group) {
          group = {
            key: unknownKey,
            label: unknownLabel,
            parts: [unknownLabel],
            registries: [],
          };
          groupMap.set(unknownKey, group);
        }
        group.registries.push(item);
      } else {
        for (const loc of validPlaces) {
          let group = groupMap.get(loc.key);
          if (!group) {
            group = {
              key: loc.key,
              label: loc.label,
              parts: loc.parts,
              registries: [],
            };
            groupMap.set(loc.key, group);
          }
          group.registries.push(item);
        }
      }
    }

    const sortedGroups = Array.from(groupMap.values()).sort((a, b) => {
      if (a.key === "__unknown__") return 1;
      if (b.key === "__unknown__") return -1;
      return a.label.localeCompare(b.label, undefined, { sensitivity: "base" });
    });

    return sortedGroups;
  });

  const allExpanded = createMemo(() => {
    const groups = locationGroups();
    if (groups.length === 0) return false;
    const exp = expandedGroups();
    return groups.every((g) => Boolean(exp[g.key]));
  });

  const toggleAllGroups = () => {
    if (allExpanded()) {
      setExpandedGroups({});
    } else {
      const all: Record<string, boolean> = {};
      for (const g of locationGroups()) {
        all[g.key] = true;
      }
      setExpandedGroups(all);
    }
  };

  const virtualRowItems = createMemo(() => {
    const groups = locationGroups();
    const cols = columns();
    const expanded = expandedGroups();
    const rows: VirtualRowItem[] = [];

    for (const group of groups) {
      const isExpanded = Boolean(expanded[group.key]);
      rows.push({
        type: "header",
        key: `header-${group.key}`,
        groupKey: group.key,
        label: group.label,
        parts: group.parts,
        count: group.registries.length,
        isCollapsed: !isExpanded,
      });

      if (isExpanded) {
        for (let i = 0; i < group.registries.length; i += cols) {
          rows.push({
            type: "cards",
            key: `cards-${group.key}-${i}`,
            groupKey: group.key,
            items: group.registries.slice(i, i + cols),
          });
        }
      }
    }

    return rows;
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
      return hasNextPage() ? virtualRowItems().length + 1 : virtualRowItems().length;
    },
    getScrollElement: () => scrollRef,
    estimateSize: (index) => {
      const row = virtualRowItems()[index];
      if (!row) return 196;
      return row.type === "header" ? 44 : 196;
    },
    gap: 16,
    overscan: 4,
  });

  createEffect(() => {
    const virtualItems = virtualizer.getVirtualItems();
    if (!virtualItems.length) return;

    const lastRenderedItem = virtualItems[virtualItems.length - 1];

    if (lastRenderedItem.index >= virtualRowItems().length - 1 && hasNextPage() && !isFetching()) {
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

      <div class="flex flex-1 min-h-0 overflow-hidden">
        <RegistryFilters
          width={sidebarWidth()}
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
        <ResizeHandle vertical={true} onPointerDown={onSidebarResizePointerDown} />

        <main class="flex-1 flex flex-col gap-6 min-w-0 min-h-0 px-6 pt-6 pb-4 overflow-hidden">
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

          <Show when={!isFetching() && items().length > 0 && locationGroups().length > 0}>
            <div class="flex items-center justify-between text-[12px] text-dim select-none -mb-3 px-1">
              <span>
                {locationGroups().length} {locationGroups().length === 1 ? t("home.placesLabel") : t("infoPanel.placesLabel")}
              </span>
              <button
                type="button"
                onClick={toggleAllGroups}
                class="text-[12px] text-accent hover:text-accent-hover font-medium transition-colors cursor-pointer"
              >
                {allExpanded() ? t("home.collapseAll") : t("home.expandAll")}
              </button>
            </div>
          </Show>

          <div
            ref={scrollRef}
            class="flex-1 overflow-y-auto pr-2 pb-4 min-h-0 scrollbar-thin scrollbar-thumb-subtle hover:scrollbar-thumb-subtle-md focus-visible:outline-none"
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
                      when={virtualRow.index < virtualRowItems().length}
                      fallback={
                        <div class="w-full flex justify-center items-center h-[196px] text-dim">
                          <Icon icon="lucide:loader-2" class="animate-spin" width="24" height="24" />
                        </div>
                      }
                    >
                      {(() => {
                        const row = () => virtualRowItems()[virtualRow.index];
                        return (
                          <Show
                            when={row()?.type === "header"}
                            fallback={
                              <div
                                class="grid"
                                style={{
                                  "grid-template-columns": `repeat(${columns()}, minmax(0, 1fr))`,
                                  gap: "16px",
                                }}
                              >
                                <For each={(row() as { type: "cards"; items: RegistryMeta[] })?.items}>
                                  {(item) => (
                                    <RegistryCard
                                      registry={item}
                                      targetImageId={extractedImageNumber()}
                                      onContextMenu={(e) => handleRegistryContextMenu(e, item)}
                                    />
                                  )}
                                </For>
                              </div>
                            }
                          >
                            <LocationGroupHeader
                              label={(row() as any).label}
                              parts={(row() as any).parts}
                              count={(row() as any).count}
                              isCollapsed={(row() as any).isCollapsed}
                              onToggle={() => toggleGroup((row() as any).groupKey)}
                            />
                          </Show>
                        );
                      })()}
                    </Show>
                  </div>
                )}
              </For>
            </div>
          </div>
        </main>
      </div>

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
