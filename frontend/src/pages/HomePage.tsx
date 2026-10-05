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
import type { AvailableOption, RegistryMeta } from "../types/registry";
import { isTauri } from "@tauri-apps/api/core";
import { getCurrent, onOpenUrl } from "@tauri-apps/plugin-deep-link";
import { openUrl } from "@tauri-apps/plugin-opener";

import { useTabs } from "../contexts/TabsContext";

type VirtualRowItem =
  | {
      type: "header";
      key: string;
      groupKey: string;
      label: string;
      parts: string[];
      count: number;
      isCollapsed: boolean;
      group: AvailableOption;
    }
  | {
      type: "loading";
      key: string;
      groupKey: string;
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

  // Data state
  const [groups, setGroups] = createSignal<AvailableOption[]>([]);
  const [groupRegistries, setGroupRegistries] = createSignal<Record<string, RegistryMeta[]>>({});
  const [loadingGroups, setLoadingGroups] = createSignal<Record<string, boolean>>({});
  const [isFetching, setIsFetching] = createSignal(false);

  const allRegistries = createMemo(() => {
    return Object.values(groupRegistries()).flat();
  });

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
      loadLocationGroups();
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
      return allRegistries().some((r) =>
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
    const reg = allRegistries().find(
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
      await loadLocationGroups();

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
        api.getRegistries({
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

  const currentFilters = () => ({
    search_term: searchQuery(),
    source_type: selectedType(),
    place: selectedPlace(),
    collection: selectedCollection(),
    date_from: dateFrom(),
    date_to: dateTo(),
  });

  const [expandedGroups, setExpandedGroups] = createSignal<Record<string, boolean>>({});

  const loadRegistriesForGroup = async (group: AvailableOption) => {
    if (loadingGroups()[group.key] || groupRegistries()[group.key]) return;

    setLoadingGroups((prev) => ({ ...prev, [group.key]: true }));
    try {
      const isUnknown = group.key === "__unknown__" || !group.parts || group.parts.length === 0;
      const res = await api.getRegistries({
        cursor: null,
        limit: 100,
        filters: {
          ...currentFilters(),
          location: isUnknown ? null : group.parts,
          is_unknown_location: isUnknown,
        },
      });
      setGroupRegistries((prev) => ({ ...prev, [group.key]: res.data }));
    } catch (err) {
      console.error(`Failed to load registries for group ${group.key}:`, err);
    } finally {
      setLoadingGroups((prev) => ({ ...prev, [group.key]: false }));
    }
  };

  const toggleGroup = (group: AvailableOption) => {
    const willExpand = !expandedGroups()[group.key];
    setExpandedGroups((prev) => ({
      ...prev,
      [group.key]: willExpand,
    }));
    if (willExpand && !groupRegistries()[group.key]) {
      loadRegistriesForGroup(group);
    }
  };

  const allExpanded = createMemo(() => {
    const currentGroups = groups();
    if (currentGroups.length === 0) return false;
    const exp = expandedGroups();
    return currentGroups.every((g) => Boolean(exp[g.key]));
  });

  const toggleAllGroups = () => {
    if (allExpanded()) {
      setExpandedGroups({});
    } else {
      const all: Record<string, boolean> = {};
      const grps = groups();
      for (const g of grps) {
        all[g.key] = true;
        if (!groupRegistries()[g.key]) {
          loadRegistriesForGroup(g);
        }
      }
      setExpandedGroups(all);
    }
  };

  const virtualRowItems = createMemo(() => {
    const currentGroups = groups();
    const cols = columns();
    const expanded = expandedGroups();
    const regMap = groupRegistries();
    const loadingMap = loadingGroups();
    const rows: VirtualRowItem[] = [];

    for (const group of currentGroups) {
      const isExpanded = Boolean(expanded[group.key]);
      const isUnknown = group.key === "__unknown__" || !group.parts || group.parts.length === 0;
      const parts = isUnknown ? [t("home.unknownLocation")] : (group.parts ?? []);
      const label = isUnknown ? t("home.unknownLocation") : group.label;

      rows.push({
        type: "header",
        key: `header-${group.key}`,
        groupKey: group.key,
        label,
        parts,
        count: group.count,
        isCollapsed: !isExpanded,
        group,
      });

      if (isExpanded) {
        const items = regMap[group.key];
        const isLoading = loadingMap[group.key];

        if (isLoading && (!items || items.length === 0)) {
          rows.push({
            type: "loading",
            key: `loading-${group.key}`,
            groupKey: group.key,
          });
        } else if (items && items.length > 0) {
          for (let i = 0; i < items.length; i += cols) {
            rows.push({
              type: "cards",
              key: `cards-${group.key}-${i}`,
              groupKey: group.key,
              items: items.slice(i, i + cols),
            });
          }
        }
      }
    }

    return rows;
  });

  let lastFetchId = 0;
  const loadLocationGroups = async () => {
    const currentFetchId = ++lastFetchId;
    setIsFetching(true);

    try {
      const filters = currentFilters();
      const res = await api.getAvailablePlaces(filters);

      if (currentFetchId !== lastFetchId) return;

      setGroups(res);
      setGroupRegistries({});
      setLoadingGroups({});
    } catch (err) {
      if (currentFetchId === lastFetchId) {
        console.error("Failed to fetch location groups:", err);
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
      setExpandedGroups({});
      untrack(() => loadLocationGroups());
    }
    return currentDeps;
  }, "");

  const virtualizer = createVirtualizer({
    get count() {
      return virtualRowItems().length;
    },
    getScrollElement: () => scrollRef,
    estimateSize: (index) => {
      const row = virtualRowItems()[index];
      if (!row) return 196;
      if (row.type === "header") return 44;
      if (row.type === "loading") return 64;
      return 196;
    },
    gap: 16,
    overscan: 4,
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

          <Show when={!isFetching() && groups().length > 0}>
            <div class="flex items-center justify-between text-[12px] text-dim select-none -mb-3 px-1">
              <span>
                {groups().length} {groups().length === 1 ? t("home.placesLabel") : t("infoPanel.placesLabel")}
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
            <Show when={!isFetching() && groups().length === 0}>
              <div class="py-16 flex flex-col items-center justify-center text-dim border-2 border-dashed border-subtle rounded-xl h-full">
                <Icon icon="lucide:folder-search" class="w-12 h-12 mb-3 text-subtle-md" />
                <p>{t("home.noResults")}</p>
              </div>
            </Show>

            <Show when={isFetching() && groups().length === 0}>
              <div class="py-16 flex flex-col items-center justify-center text-dim h-full">
                <Icon icon="lucide:loader-2" class="w-8 h-8 animate-spin text-accent mb-3" />
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
                    {(() => {
                      const row = () => virtualRowItems()[virtualRow.index];
                      return (
                        <Switch>
                          <Match when={row()?.type === "header"}>
                            <LocationGroupHeader
                              label={(row() as any).label}
                              parts={(row() as any).parts}
                              count={(row() as any).count}
                              isCollapsed={(row() as any).isCollapsed}
                              onToggle={() => toggleGroup((row() as any).group)}
                            />
                          </Match>
                          <Match when={row()?.type === "loading"}>
                            <div class="flex items-center justify-center py-6 text-dim">
                              <Icon icon="lucide:loader-2" class="w-5 h-5 animate-spin text-accent" />
                            </div>
                          </Match>
                          <Match when={row()?.type === "cards"}>
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
                          </Match>
                        </Switch>
                      );
                    })()}
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
            loadLocationGroups();
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
