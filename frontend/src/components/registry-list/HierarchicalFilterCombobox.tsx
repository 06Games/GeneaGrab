import { createEffect, createMemo, createSignal, For, Show } from "solid-js";
import { Popover } from "@kobalte/core/popover";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import type { AvailableOption } from "../../types/registry";

export interface FilterTreeNode {
  id: string;
  name: string;
  path: string[];
  key: string;
  directCount: number;
  totalCount: number;
  children: FilterTreeNode[];
  depth: number;
  isUnknown?: boolean;
}

export interface HierarchicalFilterComboboxProps {
  label: string;
  placeholder?: string;
  searchPlaceholder?: string;
  options: AvailableOption[];
  loading?: boolean;

  // Single select mode
  value?: string;
  onChange?: (val: string) => void;

  // Multi select mode
  multiple?: boolean;
  values?: string[];
  onMultipleChange?: (vals: string[]) => void;

  icon?: string;
  isPlace?: boolean;
  class?: string;
}

function normalizeStr(str: string): string {
  return str
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim();
}

export function buildFilterTree(
  options: AvailableOption[]
): {
  roots: FilterTreeNode[];
  allNodes: FilterTreeNode[];
  nodeMap: Map<string, FilterTreeNode>;
  unknownNode: FilterTreeNode | null;
} {
  const rootMap = new Map<string, FilterTreeNode>();
  const allNodes: FilterTreeNode[] = [];
  const nodeMap = new Map<string, FilterTreeNode>();
  let unknownNode: FilterTreeNode | null = null;

  for (const opt of options) {
    if (opt.key === "__unknown__") {
      unknownNode = {
        id: "__unknown__",
        name: "__unknown__",
        path: [],
        key: "__unknown__",
        directCount: opt.count,
        totalCount: opt.count,
        children: [],
        depth: 0,
        isUnknown: true,
      };
      allNodes.push(unknownNode);
      nodeMap.set("__unknown__", unknownNode);
      continue;
    }

    const parts =
      opt.parts && opt.parts.length > 0
        ? opt.parts
        : opt.label.includes(" > ")
        ? opt.label.split(" > ").map((s) => s.trim()).filter(Boolean)
        : [opt.label || opt.key];

    if (parts.length === 0) continue;

    let parentNode: FilterTreeNode | null = null;
    const currentPath: string[] = [];

    for (let depth = 0; depth < parts.length; depth++) {
      const partName = parts[depth];
      currentPath.push(partName);
      const pathKey = currentPath.join(" > ");
      const isLeaf = depth === parts.length - 1;

      let node: FilterTreeNode | undefined;

      if (depth === 0) {
        node = rootMap.get(partName.toLowerCase());
        if (!node) {
          node = {
            id: pathKey,
            name: partName,
            path: [...currentPath],
            key: pathKey,
            directCount: 0,
            totalCount: 0,
            children: [],
            depth: 0,
          };
          rootMap.set(partName.toLowerCase(), node);
          allNodes.push(node);
          nodeMap.set(pathKey.toLowerCase(), node);
        }
      } else {
        node = parentNode!.children.find(
          (c) => c.name.toLowerCase() === partName.toLowerCase()
        );
        if (!node) {
          node = {
            id: pathKey,
            name: partName,
            path: [...currentPath],
            key: pathKey,
            directCount: 0,
            totalCount: 0,
            children: [],
            depth,
          };
          parentNode!.children.push(node);
          allNodes.push(node);
          nodeMap.set(pathKey.toLowerCase(), node);
        }
      }

      node.totalCount += opt.count;
      if (isLeaf) {
        node.directCount += opt.count;
      }

      parentNode = node;
    }
  }

  // Sort nodes alphabetically
  const sortNodes = (nodes: FilterTreeNode[]) => {
    nodes.sort((a, b) =>
      a.name.localeCompare(b.name, undefined, { sensitivity: "base" })
    );
    for (const n of nodes) {
      if (n.children.length > 0) {
        sortNodes(n.children);
      }
    }
  };

  const roots = Array.from(rootMap.values());
  sortNodes(roots);

  return { roots, allNodes, nodeMap, unknownNode };
}

export const HierarchicalFilterCombobox = (
  props: HierarchicalFilterComboboxProps
) => {
  const { t } = useI18n();
  const [isOpen, setIsOpen] = createSignal(false);
  const [searchQuery, setSearchQuery] = createSignal("");
  const [expandedIds, setExpandedIds] = createSignal<Set<string>>(new Set());

  let searchInputRef: HTMLInputElement | undefined;

  const tree = createMemo(() =>
    buildFilterTree(props.options)
  );

  const selectedKeysSet = createMemo(() => {
    if (props.multiple) {
      return new Set(props.values ?? []);
    }
    return props.value ? new Set([props.value]) : new Set<string>();
  });

  const isSelected = (key: string) => selectedKeysSet().has(key);

  const isExpanded = (id: string) => expandedIds().has(id);

  // Auto-expand path of selected item and optionally top roots
  createEffect(() => {
    const val = props.value;
    const currentTree = tree();
    const newExpanded = new Set(expandedIds());

    // Expand ancestors of the active selection
    if (val && val !== "__unknown__") {
      const parts = val.split(" > ").map((s) => s.trim());
      const accum: string[] = [];
      for (let i = 0; i < parts.length - 1; i++) {
        accum.push(parts[i]);
        newExpanded.add(accum.join(" > "));
      }
    }

    // Default expand root nodes if not too many
    if (newExpanded.size === 0 && currentTree.roots.length <= 6) {
      for (const r of currentTree.roots) {
        if (r.children.length > 0) {
          newExpanded.add(r.id);
        }
      }
    }

    setExpandedIds(newExpanded);
  });

  // Focus search input when popover opens
  createEffect(() => {
    if (isOpen()) {
      setSearchQuery("");
      setTimeout(() => {
        searchInputRef?.focus();
      }, 50);
    }
  });

  const toggleExpand = (nodeId: string, e: MouseEvent) => {
    e.stopPropagation();
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(nodeId)) {
        next.delete(nodeId);
      } else {
        next.add(nodeId);
      }
      return next;
    });
  };

  const expandAll = () => {
    const all = new Set<string>();
    for (const node of tree().allNodes) {
      if (node.children.length > 0) {
        all.add(node.id);
      }
    }
    setExpandedIds(all);
  };

  const collapseAll = () => {
    setExpandedIds(new Set<string>());
  };

  const areAllExpanded = createMemo(() => {
    const nodesWithChildren = tree().allNodes.filter(
      (n) => n.children.length > 0
    );
    if (nodesWithChildren.length === 0) return false;
    return nodesWithChildren.every((n) => expandedIds().has(n.id));
  });

  const handleSelect = (node: FilterTreeNode) => {
    if (props.multiple) {
      const current = props.values ? [...props.values] : [];
      const idx = current.indexOf(node.key);
      if (idx >= 0) {
        current.splice(idx, 1);
      } else {
        current.push(node.key);
      }
      props.onMultipleChange?.(current);
      props.onChange?.(current[0] ?? "");
    } else {
      props.onChange?.(node.key);
      setIsOpen(false);
    }
  };

  const handleClear = (e?: MouseEvent) => {
    e?.stopPropagation();
    if (props.multiple) {
      props.onMultipleChange?.([]);
      props.onChange?.("");
    } else {
      props.onChange?.("");
    }
  };

  // Find info about the currently selected item for the trigger
  const selectedNodeInfo = createMemo(() => {
    const val = props.value;
    if (!val) return null;
    if (val === "__unknown__") {
      return {
        name: t("home.unknownLocation"),
        parentPath: "",
        count: tree().unknownNode?.totalCount,
        isUnknown: true,
      };
    }

    const found = tree().nodeMap.get(val.toLowerCase());
    if (found) {
      const parentParts = found.path.slice(0, -1);
      return {
        name: found.name,
        parentPath: parentParts.join(" › "),
        count: found.totalCount,
        isUnknown: false,
      };
    }

    // Fallback: parse from key string
    const parts = val.split(" > ").map((s) => s.trim());
    const leaf = parts[parts.length - 1];
    const parentParts = parts.slice(0, -1);
    return {
      name: leaf,
      parentPath: parentParts.join(" › "),
      count: undefined,
      isUnknown: false,
    };
  });

  // Flat search results when querying
  const searchResults = createMemo(() => {
    const q = normalizeStr(searchQuery());
    if (!q) return [];

    const results: { node: FilterTreeNode; score: number }[] = [];

    for (const node of tree().allNodes) {
      if (node.isUnknown) {
        const unknownLabel = normalizeStr(t("home.unknownLocation"));
        if (unknownLabel.includes(q)) {
          results.push({ node, score: unknownLabel === q ? 100 : 50 });
        }
        continue;
      }

      const nameNorm = normalizeStr(node.name);
      const pathNorm = normalizeStr(node.path.join(" > "));

      if (nameNorm === q) {
        results.push({ node, score: 100 });
      } else if (nameNorm.startsWith(q)) {
        results.push({ node, score: 80 });
      } else if (nameNorm.includes(q)) {
        results.push({ node, score: 60 });
      } else if (pathNorm.includes(q)) {
        results.push({ node, score: 40 });
      }
    }

    results.sort((a, b) => b.score - a.score || a.node.depth - b.node.depth);
    return results.map((r) => r.node);
  });

  const renderHighlight = (text: string, query: string) => {
    const q = query.trim();
    if (!q) return text;
    const lower = text.toLowerCase();
    const qLower = q.toLowerCase();
    const idx = lower.indexOf(qLower);
    if (idx === -1) return text;

    const before = text.slice(0, idx);
    const match = text.slice(idx, idx + q.length);
    const after = text.slice(idx + q.length);

    return (
      <>
        {before}
        <span class="text-accent font-bold underline decoration-accent/40">{match}</span>
        {after}
      </>
    );
  };

  // Recursive Tree Node Renderer
  const TreeNodeRow = (rowProps: { node: FilterTreeNode }) => {
    const node = () => rowProps.node;
    const selected = () => isSelected(node().key);
    const expanded = () => isExpanded(node().id);
    const hasChildren = () => node().children.length > 0;

    return (
      <div class="flex flex-col">
        <div
          class="group flex items-center gap-1.5 py-1 px-2 rounded-lg text-[13px] select-none cursor-pointer transition-colors"
          classList={{
            "bg-accent-bg text-accent-text font-medium": selected(),
            "hover:bg-hover text-main": !selected(),
          }}
          style={{ "padding-left": `${node().depth * 14 + 8}px` }}
          onClick={() => handleSelect(node())}
        >
          {/* Chevron expand/collapse toggle */}
          <Show
            when={hasChildren()}
            fallback={<span class="w-4 h-4 flex-shrink-0" />}
          >
            <button
              type="button"
              onClick={(e) => toggleExpand(node().id, e)}
              class="w-4 h-4 flex items-center justify-center rounded hover:bg-black/10 dark:hover:bg-white/10 text-subtle-md hover:text-main transition-transform cursor-pointer flex-shrink-0"
              title={expanded() ? t("home.collapseAll") : t("home.expandAll")}
            >
              <Icon
                icon="lucide:chevron-right"
                class="w-3.5 h-3.5 transition-transform duration-150"
                classList={{ "rotate-90": expanded() }}
              />
            </button>
          </Show>

          {/* Multiple checkbox */}
          <Show when={props.multiple}>
            <div class="flex items-center justify-center flex-shrink-0">
              <Icon
                icon={selected() ? "lucide:check-square" : "lucide:square"}
                class={
                  selected()
                    ? "w-3.5 h-3.5 text-accent"
                    : "w-3.5 h-3.5 text-subtle-md group-hover:text-dim"
                }
              />
            </div>
          </Show>

          {/* Node Icon */}
          <Icon
            icon={
              node().isUnknown
                ? "lucide:help-circle"
                : props.icon ?? "lucide:map-pin"
            }
            class="w-3.5 h-3.5 flex-shrink-0"
            classList={{
              "text-accent": selected(),
              "text-muted": node().isUnknown,
              "text-dim group-hover:text-main":
                !selected() && !node().isUnknown,
            }}
          />

          {/* Node Name */}
          <span
            class="flex-1 min-w-0 truncate"
            classList={{
              "italic text-muted": node().isUnknown,
              "font-medium": selected(),
            }}
            title={node().name}
          >
            {node().isUnknown ? t("home.unknownLocation") : node().name}
          </span>

          {/* Count Badge */}
          <Show when={node().totalCount > 0}>
            <span
              class="px-1.5 py-0.2 rounded-full border text-[10px] tabular-nums font-semibold flex-shrink-0 ml-1"
              classList={{
                "bg-accent/15 border-accent/30 text-accent": selected(),
                "bg-tinted border-subtle text-dim": !selected(),
              }}
            >
              {node().totalCount}
            </span>
          </Show>

          {/* Checkmark in single select mode */}
          <Show when={!props.multiple && selected()}>
            <Icon
              icon="lucide:check"
              class="w-3.5 h-3.5 text-accent flex-shrink-0 ml-0.5"
            />
          </Show>
        </div>

        {/* Recursive Children with indent guideline */}
        <Show when={hasChildren() && expanded()}>
          <div class="flex flex-col relative border-l border-subtle/50 ml-4 pl-0.5">
            <For each={node().children}>
              {(child) => <TreeNodeRow node={child} />}
            </For>
          </div>
        </Show>
      </div>
    );
  };

  const hasSelection = createMemo(() => {
    if (props.multiple) {
      return (props.values?.length ?? 0) > 0;
    }
    return Boolean(props.value);
  });

  return (
    <Popover
      open={isOpen()}
      onOpenChange={setIsOpen}
      placement="bottom-start"
      gutter={4}
      sameWidth={true}
    >
      <div
        class={["flex flex-col gap-1.5", props.class]
          .filter(Boolean)
          .join(" ")}
      >
        {/* Label and Clear Header */}
        <div class="flex items-center justify-between">
          <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
            {props.label}
          </label>
          <Show when={hasSelection()}>
            <button
              type="button"
              onClick={() => handleClear()}
              class="text-[11px] text-muted hover:text-main cursor-pointer"
            >
              <Icon icon="lucide:x" class="w-3 h-3" />
            </button>
          </Show>
        </div>

        {/* Popover Trigger Control */}
        <div class="relative flex items-center w-full">
          <Popover.Anchor class="w-full">
            <Popover.Trigger
              as="div"
              role="button"
              tabIndex={0}
              class="relative flex items-center justify-between w-full pl-9 pr-14 py-2 rounded-lg border bg-tinted text-[13px] text-main cursor-pointer transition-all select-none outline-none"
              classList={{
                "border-accent ring-2 ring-accent/15": isOpen(),
                "border-subtle hover:border-subtle-md": !isOpen(),
              }}
            >
              {/* Left icon */}
              <div class="absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none flex items-center">
                <Icon
                  icon={
                    selectedNodeInfo()?.isUnknown
                      ? "lucide:help-circle"
                      : props.icon ?? "lucide:map-pin"
                  }
                  class="w-4 h-4 transition-colors"
                  classList={{
                    "text-accent": hasSelection(),
                    "text-subtle-md": !hasSelection(),
                  }}
                />
              </div>

              {/* Display text */}
              <div class="flex items-center gap-1.5 min-w-0 pr-1 truncate">
                <Show
                  when={hasSelection()}
                  fallback={
                    <span class="text-subtle-md truncate">
                      {props.placeholder ?? props.label}
                    </span>
                  }
                >
                  <Show
                    when={!props.multiple}
                    fallback={
                      <span class="font-medium text-main">
                        {props.values?.length} {t("home.selected")}
                      </span>
                    }
                  >
                    <div class="flex flex-col min-w-0 leading-tight">
                      <div class="flex items-center gap-1 min-w-0">
                        <span
                          class="font-medium text-main truncate"
                          classList={{
                            "italic text-muted": selectedNodeInfo()?.isUnknown,
                          }}
                        >
                          {selectedNodeInfo()?.name}
                        </span>
                        <Show when={selectedNodeInfo()?.count !== undefined}>
                          <span class="text-[10px] text-dim tabular-nums">
                            ({selectedNodeInfo()?.count})
                          </span>
                        </Show>
                      </div>
                      <Show when={selectedNodeInfo()?.parentPath}>
                        <span class="text-[10px] text-dim truncate max-w-[160px]">
                          {selectedNodeInfo()?.parentPath}
                        </span>
                      </Show>
                    </div>
                  </Show>
                </Show>
              </div>

              {/* Right indicators & buttons */}
              <div class="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-0.5">
                <Show when={props.loading}>
                  <Icon
                    icon="lucide:loader-2"
                    class="w-4 h-4 text-accent animate-spin mr-1"
                  />
                </Show>

                <Show when={!props.loading && hasSelection()}>
                  <button
                    type="button"
                    onClick={handleClear}
                    class="p-1 rounded text-subtle-md hover:text-main transition-colors cursor-pointer"
                    title={t("home.clear")}
                  >
                    <Icon icon="lucide:x" class="w-3.5 h-3.5" />
                  </button>
                </Show>

                <div class="p-1 text-subtle-md hover:text-main transition-colors flex items-center">
                  <Icon icon="lucide:chevrons-up-down" class="w-3.5 h-3.5" />
                </div>
              </div>
            </Popover.Trigger>
          </Popover.Anchor>
        </div>

        {/* Popover Content */}
        <Popover.Portal>
          <Popover.Content class="z-50 w-full min-w-[280px] max-w-[340px] bg-panel border border-subtle rounded-xl shadow-xl shadow-black/15 overflow-hidden animate-in fade-in zoom-in-95 duration-100 flex flex-col outline-none">
            {/* Search Input Header */}
            <div class="p-2 border-b border-subtle flex flex-col gap-1.5 bg-tinted/50">
              <div class="relative w-full">
                <Icon
                  icon="lucide:search"
                  class="absolute left-2.5 top-1/2 -translate-y-1/2 text-subtle-md w-3.5 h-3.5 pointer-events-none"
                />
                <input
                  ref={searchInputRef}
                  type="text"
                  value={searchQuery()}
                  onInput={(e) => setSearchQuery(e.currentTarget.value)}
                  placeholder={
                    props.searchPlaceholder ??
                    (props.isPlace
                      ? t("home.searchPlace")
                      : t("home.searchCollection"))
                  }
                  class="w-full pl-8 pr-7 py-1.5 rounded-lg border border-subtle bg-panel text-[12px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-1 focus:ring-accent/15 outline-none transition-all"
                />
                <Show when={searchQuery()}>
                  <button
                    type="button"
                    onClick={() => setSearchQuery("")}
                    class="absolute right-2 top-1/2 -translate-y-1/2 text-subtle-md hover:text-main p-0.5 rounded cursor-pointer"
                  >
                    <Icon icon="lucide:x" class="w-3 h-3" />
                  </button>
                </Show>
              </div>

              {/* Action bar (Expand/Collapse all when not searching) */}
              <div class="flex items-center justify-between text-[11px] text-dim px-0.5">
                <Show
                  when={!searchQuery().trim()}
                  fallback={
                    <span>
                      {searchResults().length}{" "}
                      {searchResults().length === 1
                        ? t("home.match")
                        : t("home.matches")}
                    </span>
                  }
                >
                  <span>
                    {tree().allNodes.length}{" "}
                    {props.isPlace
                      ? t("home.locationsCount")
                      : t("home.collectionsCount")}
                  </span>
                  <div class="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() =>
                        areAllExpanded() ? collapseAll() : expandAll()
                      }
                      class="flex items-center gap-1 hover:text-main text-muted cursor-pointer transition-colors"
                    >
                      <Icon
                        icon={
                          areAllExpanded()
                            ? "lucide:fold-vertical"
                            : "lucide:unfold-vertical"
                        }
                        class="w-3 h-3"
                      />
                      <span>
                        {areAllExpanded()
                          ? t("home.collapseAll")
                          : t("home.expandAll")}
                      </span>
                    </button>
                  </div>
                </Show>
              </div>
            </div>

            {/* List Body */}
            <div class="max-h-64 overflow-y-auto p-1.5 flex flex-col gap-0.5 scrollbar-thin scrollbar-thumb-subtle">
              <Show when={props.loading}>
                <div class="flex items-center justify-center p-4 text-dim text-[12px] gap-2">
                  <Icon
                    icon="lucide:loader-2"
                    class="w-3.5 h-3.5 animate-spin text-accent"
                  />
                  <span>{t("home.loading")}</span>
                </div>
              </Show>

              {/* Tree View (when not searching) */}
              <Show when={!props.loading && !searchQuery().trim()}>
                <Show
                  when={tree().roots.length > 0 || tree().unknownNode}
                  fallback={
                    <div class="px-3 py-6 text-center text-dim text-[12px]">
                      {t("home.noOptions")}
                    </div>
                  }
                >
                  <For each={tree().roots}>
                    {(rootNode) => <TreeNodeRow node={rootNode} />}
                  </For>

                  {/* Unknown location row at bottom */}
                  <Show when={tree().unknownNode}>
                    <div class="border-t border-subtle/50 mt-1 pt-1">
                      <TreeNodeRow node={tree().unknownNode!} />
                    </div>
                  </Show>
                </Show>
              </Show>

              {/* Flat Search Results View (when searching) */}
              <Show when={!props.loading && searchQuery().trim()}>
                <Show
                  when={searchResults().length > 0}
                  fallback={
                    <div class="px-3 py-6 text-center text-dim text-[12px]">
                      {t("home.noMatches", { query: searchQuery() })}
                    </div>
                  }
                >
                  <For each={searchResults()}>
                    {(node) => {
                      const selected = () => isSelected(node.key);
                      return (
                        <div
                          class="flex items-center justify-between py-1.5 px-2 rounded-lg text-[13px] select-none cursor-pointer transition-colors group"
                          classList={{
                            "bg-accent-bg text-accent-text font-medium": selected(),
                            "hover:bg-hover text-main": !selected(),
                          }}
                          onClick={() => handleSelect(node)}
                        >
                          <div class="flex items-center gap-2 min-w-0 pr-2">
                            <Show when={props.multiple}>
                              <Icon
                                icon={
                                  selected()
                                    ? "lucide:check-square"
                                    : "lucide:square"
                                }
                                class={
                                  selected()
                                    ? "w-3.5 h-3.5 text-accent"
                                    : "w-3.5 h-3.5 text-subtle-md"
                                }
                              />
                            </Show>
                            <Icon
                              icon={
                                node.isUnknown
                                  ? "lucide:help-circle"
                                  : props.icon ?? "lucide:map-pin"
                              }
                              class="w-3.5 h-3.5 flex-shrink-0"
                              classList={{
                                "text-accent": selected(),
                                "text-muted": node.isUnknown,
                                "text-dim group-hover:text-main":
                                  !selected() && !node.isUnknown,
                              }}
                            />
                            <div class="flex flex-col min-w-0">
                              <span
                                class="truncate font-medium leading-tight"
                                classList={{ "italic text-muted": node.isUnknown }}
                              >
                                {node.isUnknown
                                  ? t("home.unknownLocation")
                                  : renderHighlight(node.name, searchQuery())}
                              </span>
                              <Show when={node.path.length > 1}>
                                <span class="text-[10px] text-dim truncate">
                                  {node.path.slice(0, -1).join(" › ")}
                                </span>
                              </Show>
                            </div>
                          </div>

                          <div class="flex items-center gap-1.5 flex-shrink-0">
                            <span
                              class="px-1.5 py-0.2 rounded-full border text-[10px] tabular-nums font-semibold"
                              classList={{
                                "bg-accent/15 border-accent/30 text-accent": selected(),
                                "bg-tinted border-subtle text-dim": !selected(),
                              }}
                            >
                              {node.totalCount}
                            </span>
                            <Show when={!props.multiple && selected()}>
                              <Icon
                                icon="lucide:check"
                                class="w-3.5 h-3.5 text-accent"
                              />
                            </Show>
                          </div>
                        </div>
                      );
                    }}
                  </For>
                </Show>
              </Show>
            </div>
          </Popover.Content>
        </Popover.Portal>
      </div>
    </Popover>
  );
};
