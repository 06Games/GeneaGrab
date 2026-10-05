import { createMemo, createSignal, For, JSX, Show } from "solid-js";
import { Combobox, useComboboxContext } from "@kobalte/core/combobox";
import { Icon } from "@iconify-icon/solid";
import type { AvailableOption } from "../../types/registry";

export interface FilterComboboxProps<T = AvailableOption> {
  label: string;
  placeholder?: string;
  options: T[];
  loading?: boolean;

  // Single selection mode
  value?: string;
  onChange?: (val: string) => void;

  // Multi selection mode (future-ready)
  multiple?: boolean;
  values?: string[];
  onMultipleChange?: (vals: string[]) => void;

  // Customization
  icon?: string;
  getOptionKey?: (opt: T) => string;
  getOptionLabel?: (opt: T) => string;
  customFilter?: (opt: T, query: string) => boolean;
  renderOption?: (opt: T, isSelected: boolean) => JSX.Element;
  emptyMessage?: string;
  class?: string;
}

function ComboboxEmptyState(props: { emptyMessage?: string; loading?: boolean; optionsCount: number }) {
  const context = useComboboxContext();
  const isEmpty = createMemo(() => {
    if (props.loading) return false;
    if (props.optionsCount === 0) return true;
    return context.listState().collection().getSize() === 0;
  });

  return (
    <>
      <Show when={props.loading}>
        <div class="flex items-center justify-center p-3 text-dim text-[12px] gap-2">
          <Icon icon="lucide:loader-2" class="w-3.5 h-3.5 animate-spin text-accent" />
          <span>Loading...</span>
        </div>
      </Show>
      <Show when={isEmpty()}>
        <div class="px-3 py-4 text-center text-dim text-[12px]">
          <Show
            when={Boolean(context.inputValue())}
            fallback={props.emptyMessage ?? "No options available"}
          >
            No options match "{context.inputValue()}"
          </Show>
        </div>
      </Show>
    </>
  );
}

function SingleFilterCombobox<T = AvailableOption>(props: FilterComboboxProps<T>) {
  const getKey = (opt: T): string => {
    if (!opt) return "";
    if (props.getOptionKey) return props.getOptionKey(opt);
    return (opt as any).key ?? String(opt);
  };

  const getLabel = (opt: T): string => {
    if (!opt) return "";
    if (props.getOptionLabel) return props.getOptionLabel(opt);
    return (opt as any).label ?? String(opt);
  };

  const selectedOption = createMemo(() => {
    const val = props.value;
    if (!val) return null;
    return props.options.find((opt) => getKey(opt) === val) ?? null;
  });

  const handleChange = (opt: T | null) => {
    props.onChange?.(opt ? getKey(opt) : "");
  };

  const selectedIcon = createMemo(() => {
    const opt = selectedOption();
    if (opt && props.getSelectedIcon) {
      const res = props.getSelectedIcon(opt);
      if (typeof res === "string") return { icon: res, class: "text-accent" };
      return res;
    }
    return { icon: props.icon ?? "lucide:search", class: "text-subtle-md" };
  });

  return (
    <Combobox<T>
      options={props.options}
      optionValue={getKey}
      optionTextValue={getLabel}
      optionLabel={getLabel}
      value={selectedOption()}
      onChange={handleChange}
      triggerMode="focus"
      defaultFilter={(opt, query) => {
        if (props.customFilter) return props.customFilter(opt, query);
        const q = query.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim();
        if (!q) return true;
        const lbl = getLabel(opt).toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
        const key = getKey(opt).toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
        return lbl.includes(q) || key.includes(q);
      }}
      allowsEmptyCollection={true}
      placeholder={props.placeholder}
      sameWidth={true}
      gutter={4}
      itemComponent={(itemProps) => {
        const isSelected = createMemo(() => getKey(itemProps.item.rawValue) === props.value);
        return (
          <Combobox.Item
            item={itemProps.item}
            class="px-2.5 py-2 rounded-lg text-[13px] flex items-center justify-between cursor-pointer data-highlighted:bg-hover data-selected:bg-accent-bg data-selected:text-accent-text transition-colors select-none group"
          >
            <Combobox.ItemLabel class="flex-1 min-w-0 pr-2">
              {props.renderOption
                ? props.renderOption(itemProps.item.rawValue, isSelected())
                : getLabel(itemProps.item.rawValue)}
            </Combobox.ItemLabel>
            <div class="flex items-center gap-1.5 flex-shrink-0">
              <Show when={(itemProps.item.rawValue as any).count !== undefined}>
                <span class="px-1.5 py-0.5 rounded-full bg-tinted border border-subtle text-dim text-[11px] tabular-nums font-semibold">
                  {(itemProps.item.rawValue as any).count}
                </span>
              </Show>
              <Combobox.ItemIndicator class="text-accent flex items-center">
                <Icon icon="lucide:check" class="w-3.5 h-3.5" />
              </Combobox.ItemIndicator>
            </div>
          </Combobox.Item>
        );
      }}
    >
      <div class={["flex flex-col gap-1.5", props.class].filter(Boolean).join(" ")}>
        <div class="flex items-center justify-between">
          <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
            {props.label}
          </label>
          <Show when={props.value}>
            <button
              type="button"
              onClick={() => props.onChange?.("")}
              class="text-[11px] text-muted hover:text-main cursor-pointer"
            >
              <Icon icon="lucide:x" class="w-3 h-3" />
            </button>
          </Show>
        </div>

        <Combobox.Control
          aria-label={props.label}
          class="relative flex items-center w-full rounded-lg border border-subtle bg-tinted transition-all focus-within:border-accent focus-within:ring-2 focus-within:ring-accent/15"
        >
          <div class={["absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none flex items-center transition-colors", selectedIcon().class].join(" ")}>
            <Icon icon={selectedIcon().icon} class="w-4 h-4" />
          </div>

          <Combobox.Input
            onFocus={(e) => e.currentTarget.select()}
            class="w-full pl-9 pr-14 py-2 bg-transparent text-[13px] text-main placeholder:text-subtle-md outline-none"
            placeholder={props.placeholder}
          />

          <div class="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-0.5">
            <Show when={props.loading}>
              <Icon icon="lucide:loader-2" class="w-4 h-4 text-accent animate-spin mr-1" />
            </Show>

            <Show when={!props.loading && selectedOption()}>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  props.onChange?.("");
                }}
                class="p-1 rounded text-subtle-md hover:text-main transition-colors cursor-pointer"
                title="Clear"
              >
                <Icon icon="lucide:x" class="w-3.5 h-3.5" />
              </button>
            </Show>

            <Combobox.Trigger class="p-1 rounded text-subtle-md hover:text-main transition-colors cursor-pointer flex items-center">
              <Combobox.Icon>
                <Icon icon="lucide:chevrons-up-down" class="w-3.5 h-3.5" />
              </Combobox.Icon>
            </Combobox.Trigger>
          </div>
        </Combobox.Control>

        <Combobox.Portal>
          <Combobox.Content class="z-50 w-full min-w-[240px] bg-panel border border-subtle rounded-xl shadow-xl shadow-black/10 overflow-hidden animate-in fade-in zoom-in-95 duration-100">
            <ComboboxEmptyState emptyMessage={props.emptyMessage} loading={props.loading} optionsCount={props.options.length} />
            <Combobox.Listbox class="max-h-60 overflow-y-auto p-1.5 flex flex-col gap-0.5 scrollbar-thin scrollbar-thumb-subtle" />
          </Combobox.Content>
        </Combobox.Portal>
      </div>
    </Combobox>
  );
}

function MultiFilterCombobox<T = AvailableOption>(props: FilterComboboxProps<T>) {
  const getKey = (opt: T): string => {
    if (!opt) return "";
    if (props.getOptionKey) return props.getOptionKey(opt);
    return (opt as any).key ?? String(opt);
  };

  const getLabel = (opt: T): string => {
    if (!opt) return "";
    if (props.getOptionLabel) return props.getOptionLabel(opt);
    return (opt as any).label ?? String(opt);
  };

  const selectedValues = createMemo(() => {
    if (props.values) return props.values;
    if (props.value) return [props.value];
    return [];
  });

  const selectedOptions = createMemo(() => {
    const vals = new Set(selectedValues());
    return props.options.filter((opt) => vals.has(getKey(opt)));
  });

  const handleChange = (opts: T[]) => {
    const keys = opts.map(getKey);
    props.onMultipleChange?.(keys);
    props.onChange?.(keys[0] ?? "");
  };

  return (
    <Combobox<T>
      multiple={true}
      options={props.options}
      optionValue={getKey}
      optionTextValue={getLabel}
      optionLabel={getLabel}
      value={selectedOptions()}
      onChange={handleChange}
      triggerMode="focus"
      defaultFilter={(opt, query) => {
        if (props.customFilter) return props.customFilter(opt, query);
        const q = query.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim();
        if (!q) return true;
        const lbl = getLabel(opt).toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
        const key = getKey(opt).toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
        return lbl.includes(q) || key.includes(q);
      }}
      allowsEmptyCollection={true}
      placeholder={props.placeholder}
      sameWidth={true}
      gutter={4}
      itemComponent={(itemProps) => {
        const isSelected = createMemo(() => selectedValues().includes(getKey(itemProps.item.rawValue)));
        return (
          <Combobox.Item
            item={itemProps.item}
            class="px-2.5 py-2 rounded-lg text-[13px] flex items-center justify-between cursor-pointer data-highlighted:bg-hover data-selected:bg-accent-bg data-selected:text-accent-text transition-colors select-none group"
          >
            <div class="flex items-center gap-2 flex-1 min-w-0 pr-2">
              <div class="w-4 h-4 rounded border border-subtle-md flex items-center justify-center group-data-selected:bg-accent group-data-selected:border-accent transition-colors flex-shrink-0">
                <Combobox.ItemIndicator class="text-white flex items-center">
                  <Icon icon="lucide:check" class="w-3 h-3" />
                </Combobox.ItemIndicator>
              </div>
              <Combobox.ItemLabel class="flex-1 min-w-0">
                {props.renderOption
                  ? props.renderOption(itemProps.item.rawValue, isSelected())
                  : getLabel(itemProps.item.rawValue)}
              </Combobox.ItemLabel>
            </div>
            <Show when={(itemProps.item.rawValue as any).count !== undefined}>
              <span class="px-1.5 py-0.5 rounded-full bg-tinted border border-subtle text-dim text-[11px] tabular-nums font-semibold flex-shrink-0">
                {(itemProps.item.rawValue as any).count}
              </span>
            </Show>
          </Combobox.Item>
        );
      }}
    >
      <div class={["flex flex-col gap-1.5", props.class].filter(Boolean).join(" ")}>
        <div class="flex items-center justify-between">
          <label class="text-[11px] font-semibold uppercase tracking-wider text-dim">
            {props.label}
          </label>
          <Show when={selectedValues().length > 0}>
            <button
              type="button"
              onClick={() => {
                props.onMultipleChange?.([]);
                props.onChange?.("");
              }}
              class="text-[11px] text-muted hover:text-main cursor-pointer"
            >
              <Icon icon="lucide:x" class="w-3 h-3" />
            </button>
          </Show>
        </div>

        <Combobox.Control
          aria-label={props.label}
          class="relative flex flex-wrap items-center gap-1.5 p-1.5 w-full rounded-lg border border-subtle bg-tinted transition-all focus-within:border-accent focus-within:ring-2 focus-within:ring-accent/15 min-h-[38px]"
        >
          {(state) => (
            <>
              <For each={state.selectedOptions()}>
                {(opt) => {
                  const iconRes = props.getSelectedIcon ? props.getSelectedIcon(opt) : null;
                  const iconName = typeof iconRes === "string" ? iconRes : iconRes?.icon;
                  return (
                    <span class="inline-flex items-center gap-1.5 pl-2 pr-1 py-0.5 rounded-md bg-accent-bg border border-accent-border text-accent-text text-[11px] font-medium animate-in fade-in">
                      <Show when={iconName}>
                        <Icon icon={iconName!} class="w-3 h-3 flex-shrink-0" />
                      </Show>
                      <span class="truncate max-w-[140px]">{getLabel(opt)}</span>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          state.remove(opt);
                        }}
                        class="hover:text-main p-0.5 rounded cursor-pointer transition-colors"
                        title="Remove"
                      >
                        <Icon icon="lucide:x" class="w-3 h-3" />
                      </button>
                    </span>
                  );
                }}
              </For>

              <div class="relative flex-1 min-w-[100px] flex items-center">
                <Combobox.Input
                  onFocus={(e) => e.currentTarget.select()}
                  class="w-full bg-transparent text-[13px] text-main placeholder:text-subtle-md outline-none px-1"
                  placeholder={state.selectedOptions().length > 0 ? "" : props.placeholder}
                />
              </div>

              <div class="ml-auto flex items-center gap-0.5 flex-shrink-0">
                <Show when={props.loading}>
                  <Icon icon="lucide:loader-2" class="w-4 h-4 text-accent animate-spin mr-1" />
                </Show>

                <Show when={!props.loading && state.selectedOptions().length > 0}>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      state.clear();
                    }}
                    class="p-1 rounded text-subtle-md hover:text-main transition-colors cursor-pointer"
                    title="Clear all"
                  >
                    <Icon icon="lucide:x" class="w-3.5 h-3.5" />
                  </button>
                </Show>

                <Combobox.Trigger class="p-1 rounded text-subtle-md hover:text-main transition-colors cursor-pointer flex items-center">
                  <Combobox.Icon>
                    <Icon icon="lucide:chevrons-up-down" class="w-3.5 h-3.5" />
                  </Combobox.Icon>
                </Combobox.Trigger>
              </div>
            </>
          )}
        </Combobox.Control>

        <Combobox.Portal>
          <Combobox.Content class="z-50 w-full min-w-[240px] bg-panel border border-subtle rounded-xl shadow-xl shadow-black/10 overflow-hidden animate-in fade-in zoom-in-95 duration-100">
            <ComboboxEmptyState emptyMessage={props.emptyMessage} loading={props.loading} optionsCount={props.options.length} />
            <Combobox.Listbox class="max-h-60 overflow-y-auto p-1.5 flex flex-col gap-0.5 scrollbar-thin scrollbar-thumb-subtle" />
          </Combobox.Content>
        </Combobox.Portal>
      </div>
    </Combobox>
  );
}

export function FilterCombobox<T = AvailableOption>(props: FilterComboboxProps<T>) {
  return (
    <Show
      when={props.multiple}
      fallback={<SingleFilterCombobox {...props} />}
    >
      <MultiFilterCombobox {...props} />
    </Show>
  );
}
