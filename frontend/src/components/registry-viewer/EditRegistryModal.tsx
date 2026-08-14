import { createSignal, For, Show } from "solid-js";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { Button } from "../../ui/primitives";
import { ActType, ActTypeCategory, ACT_TYPE_OPTIONS, RegistryMeta, UserRegistryMeta } from "../../types/registry";

interface EditRegistryModalProps {
  registryMeta: RegistryMeta;
  onClose: () => void;
  onSave: (meta: Partial<UserRegistryMeta>) => Promise<void>;
}

const ACT_TYPE_BADGE_STYLES: Record<ActTypeCategory, string> = {
  vital: "bg-event-vital-bg text-event-vital border-event-vital-border",
  union: "bg-event-union-bg text-event-union border-event-union-border",
  mortality: "bg-event-mortality-bg text-event-mortality border-event-mortality-border",
  census: "bg-event-census-bg text-event-census border-event-census-border",
  legal: "bg-event-legal-bg text-event-legal border-event-legal-border",
  land: "bg-event-land-bg text-event-land border-event-land-border",
  media: "bg-event-media-bg text-event-media border-event-media-border",
  military: "bg-event-military-bg text-event-military border-event-military-border",
  other: "bg-event-other-bg text-event-other border-event-other-border",
  unknown: "bg-event-other-bg text-event-other border-event-other-border",
};

export const EditRegistryModal = (props: EditRegistryModalProps) => {
  const { t } = useI18n();

  const [archiveReference, setArchiveReference] = createSignal(props.registryMeta.archive_reference ?? "");
  const [title, setTitle] = createSignal(props.registryMeta.title ?? "");
  const [subtitle, setSubtitle] = createSignal(props.registryMeta.subtitle ?? "");
  const [author, setAuthor] = createSignal(props.registryMeta.author ?? "");
  const [dateFrom, setDateFrom] = createSignal(props.registryMeta.date_from ?? "");
  const [dateTo, setDateTo] = createSignal(props.registryMeta.date_to ?? "");
  const [arkUrl, setArkUrl] = createSignal(props.registryMeta.ark_url ?? "");
  const [notes, setNotes] = createSignal(props.registryMeta.notes ?? "");

  // Places extraction
  const initialPlaces = (): string[] => {
    const p = props.registryMeta.places;
    if (!p) return [];
    const arr = Array.isArray(p) ? p : Array.from(p);
    return arr.map((item) => (Array.isArray(item) ? item.join(", ") : String(item)));
  };
  const [placesList, setPlacesList] = createSignal<string[]>(initialPlaces());
  const [newPlaceInput, setNewPlaceInput] = createSignal("");

  // Collection extraction (archival hierarchy: 1. Fonds > 2. Série > 3. Sous-série)
  const initialCollections = (): string[] => {
    const c = props.registryMeta.collection;
    if (!c) return [];
    const arr = Array.isArray(c) ? c : Array.from(c);
    return arr.map(String).filter(Boolean);
  };
  const [collectionList, setCollectionList] = createSignal<string[]>(initialCollections());
  const [newCollectionInput, setNewCollectionInput] = createSignal("");

  // Source types extraction
  const initialActTypes = (): ActType[] => {
    const st = props.registryMeta.source_types;
    if (!st) return [];
    const arr = Array.isArray(st) ? st : Array.from(st);
    return arr.map((item) => ({
      category: item.category,
      label: item.label || item.category,
    }));
  };
  const [actTypesList, setActTypesList] = createSignal<ActType[]>(initialActTypes());
  const [newTypeCategory, setNewTypeCategory] = createSignal<ActTypeCategory>("vital");
  const [newTypeLabel, setNewTypeLabel] = createSignal("");

  const [isSaving, setIsSaving] = createSignal(false);
  const [errorMsg, setErrorMsg] = createSignal<string | null>(null);

  // Safe backdrop click tracking to avoid closing during text selection drag
  let mouseDownOnBackdrop = false;

  const handleBackdropMouseDown = (e: MouseEvent) => {
    mouseDownOnBackdrop = e.target === e.currentTarget;
  };

  const handleBackdropMouseUp = (e: MouseEvent) => {
    if (mouseDownOnBackdrop && e.target === e.currentTarget) {
      props.onClose();
    }
    mouseDownOnBackdrop = false;
  };

  const handleAddActType = () => {
    const cat = newTypeCategory();
    const lbl = newTypeLabel().trim() || t(`actCategories.${cat}` as any) || cat;
    setActTypesList([...actTypesList(), { category: cat, label: lbl }]);
    setNewTypeLabel("");
  };

  const handleRemoveActType = (idx: number) => {
    setActTypesList(actTypesList().filter((_, i) => i !== idx));
  };

  const handleUpdateActTypeLabel = (idx: number, lbl: string) => {
    const updated = [...actTypesList()];
    updated[idx] = { ...updated[idx], label: lbl };
    setActTypesList(updated);
  };

  const handleUpdateActTypeCategory = (idx: number, cat: ActTypeCategory) => {
    const updated = [...actTypesList()];
    updated[idx] = { ...updated[idx], category: cat };
    setActTypesList(updated);
  };

  const handleAddPlace = () => {
    const val = newPlaceInput().trim();
    if (val && !placesList().includes(val)) {
      setPlacesList([...placesList(), val]);
      setNewPlaceInput("");
    }
  };

  const handleRemovePlace = (idx: number) => {
    setPlacesList(placesList().filter((_, i) => i !== idx));
  };

  // Collection hierarchy level operations
  const handleAddCollection = () => {
    const val = newCollectionInput().trim();
    if (!val) return;
    const parts = val.split(/[>/]/).map((s) => s.trim()).filter(Boolean);
    if (parts.length > 0) {
      setCollectionList([...collectionList(), ...parts]);
      setNewCollectionInput("");
    }
  };

  const handleUpdateCollectionLevel = (idx: number, val: string) => {
    const updated = [...collectionList()];
    updated[idx] = val;
    setCollectionList(updated);
  };

  const handleMoveCollectionUp = (idx: number) => {
    if (idx <= 0) return;
    const updated = [...collectionList()];
    const temp = updated[idx - 1];
    updated[idx - 1] = updated[idx];
    updated[idx] = temp;
    setCollectionList(updated);
  };

  const handleMoveCollectionDown = (idx: number) => {
    if (idx >= collectionList().length - 1) return;
    const updated = [...collectionList()];
    const temp = updated[idx + 1];
    updated[idx + 1] = updated[idx];
    updated[idx] = temp;
    setCollectionList(updated);
  };

  const handleRemoveCollection = (idx: number) => {
    setCollectionList(collectionList().filter((_, i) => i !== idx));
  };

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    setIsSaving(true);
    setErrorMsg(null);

    try {
      const formattedPlaces = placesList().map((p) => p.split(",").map((s) => s.trim()).filter(Boolean));
      const filteredCollection = collectionList().map((s) => s.trim()).filter(Boolean);

      await props.onSave({
        archive_reference: archiveReference().trim() || undefined,
        title: title().trim() || undefined,
        subtitle: subtitle().trim() || undefined,
        author: author().trim() || undefined,
        date_from: dateFrom().trim() || undefined,
        date_to: dateTo().trim() || undefined,
        ark_url: arkUrl().trim() || undefined,
        notes: notes().trim() || undefined,
        places: formattedPlaces.length > 0 ? formattedPlaces : undefined,
        collection: filteredCollection.length > 0 ? filteredCollection : undefined,
        source_types: actTypesList(),
      });

      props.onClose();
    } catch (err: any) {
      console.error("Failed to save registry:", err);
      setErrorMsg(err?.message || String(err));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs p-4 animate-in fade-in duration-150"
      onMouseDown={handleBackdropMouseDown}
      onMouseUp={handleBackdropMouseUp}
      onKeyDown={(e) => {
        if (e.key === "Escape") props.onClose();
      }}
      tabIndex={-1}
    >
      <div
        class="bg-panel w-full max-w-2xl max-h-[90vh] rounded-xl shadow-2xl border border-subtle overflow-hidden flex flex-col antialiased text-main select-text"
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-registry-title"
      >
        {/* Header */}
        <div class="flex items-center justify-between px-6 py-4 border-b border-subtle bg-tinted flex-shrink-0 select-none">
          <div>
            <h2 id="edit-registry-title" class="text-[16px] font-semibold text-main">
              {t("infoPanel.editModal.title")}
            </h2>
            <p class="text-[12px] text-dim mt-0.5">{t("infoPanel.editModal.subtitle")}</p>
          </div>
          <button
            type="button"
            onClick={props.onClose}
            class="w-8 h-8 rounded-lg flex items-center justify-center text-muted hover:text-main hover:bg-hover transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-accent cursor-pointer"
          >
            <Icon icon="lucide:x" width="16" height="16" class="block" />
          </button>
        </div>

        {/* Scrollable Form Body */}
        <form onSubmit={handleSubmit} class="flex-1 overflow-y-auto p-6 flex flex-col gap-6 scrollbar-thin scrollbar-thumb-subtle">
          <Show when={errorMsg()}>
            <div class="flex items-center gap-2 p-3 rounded-lg bg-danger/10 border border-danger/20 text-danger text-[13px]">
              <Icon icon="lucide:alert-circle" width="16" height="16" class="flex-shrink-0" />
              <span>{errorMsg()}</span>
            </div>
          </Show>

          {/* Section: Main Identifiers */}
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div class="flex flex-col gap-1.5 sm:col-span-2">
              <label for="reg-arch-ref" class="text-[12px] font-medium text-dim select-none">
                {t("infoPanel.archiveReference")} *
              </label>
              <input
                id="reg-arch-ref"
                type="text"
                required
                value={archiveReference()}
                onInput={(e) => setArchiveReference(e.currentTarget.value)}
                placeholder={t("infoPanel.editModal.archiveRefPlaceholder")}
                class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
              />
            </div>

            <div class="flex flex-col gap-1.5 sm:col-span-2">
              <label for="reg-title" class="text-[12px] font-medium text-dim select-none">
                {t("infoPanel.titleLabel")}
              </label>
              <input
                id="reg-title"
                type="text"
                value={title()}
                onInput={(e) => setTitle(e.currentTarget.value)}
                placeholder={t("infoPanel.editModal.titlePlaceholder")}
                class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
              />
            </div>

            <div class="flex flex-col gap-1.5">
              <label for="reg-subtitle" class="text-[12px] font-medium text-dim select-none">
                {t("infoPanel.subtitleLabel")}
              </label>
              <input
                id="reg-subtitle"
                type="text"
                value={subtitle()}
                onInput={(e) => setSubtitle(e.currentTarget.value)}
                placeholder={t("infoPanel.editModal.subtitlePlaceholder")}
                class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
              />
            </div>

            <div class="flex flex-col gap-1.5">
              <label for="reg-author" class="text-[12px] font-medium text-dim select-none">
                {t("infoPanel.authorLabel")}
              </label>
              <input
                id="reg-author"
                type="text"
                value={author()}
                onInput={(e) => setAuthor(e.currentTarget.value)}
                placeholder={t("infoPanel.editModal.authorPlaceholder")}
                class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
              />
            </div>
          </div>

          {/* Section: Period */}
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div class="flex flex-col gap-1.5">
              <label for="reg-date-from" class="text-[12px] font-medium text-dim select-none">
                {t("infoPanel.editModal.dateFromLabel")}
              </label>
              <input
                id="reg-date-from"
                type="text"
                value={dateFrom()}
                onInput={(e) => setDateFrom(e.currentTarget.value)}
                placeholder={t("infoPanel.editModal.dateFromPlaceholder")}
                class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
              />
            </div>

            <div class="flex flex-col gap-1.5">
              <label for="reg-date-to" class="text-[12px] font-medium text-dim select-none">
                {t("infoPanel.editModal.dateToLabel")}
              </label>
              <input
                id="reg-date-to"
                type="text"
                value={dateTo()}
                onInput={(e) => setDateTo(e.currentTarget.value)}
                placeholder={t("infoPanel.editModal.dateToPlaceholder")}
                class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
              />
            </div>
          </div>

          {/* Section: Record Types */}
          <div class="flex flex-col gap-3 p-4 rounded-xl border border-subtle bg-tinted/40">
            <label class="text-[13px] font-semibold text-main select-none">{t("infoPanel.editModal.typesLabel")}</label>

            {/* Existing Act Types List */}
            <div class="flex flex-col gap-2">
              <Show
                when={actTypesList().length > 0}
                fallback={
                  <div class="text-[12px] text-dim italic p-3 border border-dashed border-subtle rounded-lg text-center bg-panel/50 select-none">
                    {t("infoPanel.editModal.noTypes")}
                  </div>
                }
              >
                <For each={actTypesList()}>
                  {(item, idx) => (
                    <div class="flex items-center gap-2 p-2 rounded-lg border border-subtle bg-panel shadow-xs">
                      {/* Fixed Category Selector */}
                      <div class="relative flex-shrink-0">
                        <select
                          value={item.category}
                          onChange={(e) => handleUpdateActTypeCategory(idx(), e.currentTarget.value as ActTypeCategory)}
                          class={[
                            "h-8 pl-3 pr-7 rounded-lg border text-[12px] font-semibold appearance-none cursor-pointer focus:outline-none focus:ring-2 focus:ring-accent/20 transition-all",
                            ACT_TYPE_BADGE_STYLES[item.category],
                          ].join(" ")}
                        >
                          <For each={ACT_TYPE_OPTIONS}>
                            {(cat) => (
                              <option value={cat} class="bg-panel text-main">
                                {t(`actCategories.${cat}` as any) || cat}
                              </option>
                            )}
                          </For>
                        </select>
                        <span class="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none opacity-60">
                          <Icon icon="lucide:chevron-down" width="12" height="12" />
                        </span>
                      </div>

                      {/* Custom Free-Text Label */}
                      <div class="relative flex-1 min-w-0">
                        <input
                          type="text"
                          value={item.label || ""}
                          onInput={(e) => handleUpdateActTypeLabel(idx(), e.currentTarget.value)}
                          placeholder={t("infoPanel.editModal.typeLabelPlaceholder")}
                          class="w-full h-8 px-3 rounded-lg border border-subtle bg-tinted text-[13px] text-main font-medium placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
                        />
                      </div>

                      {/* Delete Button */}
                      <button
                        type="button"
                        onClick={() => handleRemoveActType(idx())}
                        class="w-8 h-8 rounded-lg flex items-center justify-center text-dim hover:text-danger hover:bg-danger/10 transition-colors flex-shrink-0 cursor-pointer"
                        title="Supprimer"
                      >
                        <Icon icon="lucide:trash-2" width="14" height="14" />
                      </button>
                    </div>
                  )}
                </For>
              </Show>
            </div>

            {/* Add New Act Type Input Card */}
            <div class="flex items-center gap-2 p-2 rounded-lg border border-dashed border-subtle bg-tinted/60">
              <div class="relative flex-shrink-0">
                <select
                  value={newTypeCategory()}
                  onChange={(e) => setNewTypeCategory(e.currentTarget.value as ActTypeCategory)}
                  class={[
                    "h-8 pl-3 pr-7 rounded-lg border text-[12px] font-semibold appearance-none cursor-pointer focus:outline-none focus:ring-2 focus:ring-accent/20 transition-all",
                    ACT_TYPE_BADGE_STYLES[newTypeCategory()],
                  ].join(" ")}
                >
                  <For each={ACT_TYPE_OPTIONS}>
                    {(cat) => (
                      <option value={cat} class="bg-panel text-main">
                        {t(`actCategories.${cat}` as any) || cat}
                      </option>
                    )}
                  </For>
                </select>
                <span class="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none opacity-60">
                  <Icon icon="lucide:chevron-down" width="12" height="12" />
                </span>
              </div>

              <input
                type="text"
                value={newTypeLabel()}
                onInput={(e) => setNewTypeLabel(e.currentTarget.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    handleAddActType();
                  }
                }}
                placeholder={t("infoPanel.editModal.typeLabelPlaceholder")}
                class="flex-1 h-8 px-3 rounded-lg border border-subtle bg-panel text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all min-w-0"
              />

              <Button variant="outline" size="sm" onClick={handleAddActType} class="flex-shrink-0 h-8 px-3 text-[12px]">
                <Icon icon="lucide:plus" width="13" height="13" />
                <span>{t("infoPanel.editModal.addType")}</span>
              </Button>
            </div>
          </div>

          {/* Section: Places */}
          <div class="flex flex-col gap-2">
            <label class="text-[12px] font-medium text-dim select-none">{t("infoPanel.editModal.placesLabel")}</label>
            <div class="flex flex-wrap gap-1.5 min-h-[36px] p-2 rounded-lg border border-subtle bg-tinted">
              <For each={placesList()}>
                {(place, idx) => (
                  <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-panel border border-subtle text-[12px] text-main font-medium shadow-xs max-w-full min-w-0">
                    <Icon icon="lucide:map-pin" width="11" height="11" class="text-dim flex-shrink-0" />
                    <span class="truncate">{place}</span>
                    <button
                      type="button"
                      onClick={() => handleRemovePlace(idx())}
                      class="text-dim hover:text-danger ml-0.5 focus:outline-none flex-shrink-0 cursor-pointer"
                    >
                      <Icon icon="lucide:x" width="12" height="12" class="block" />
                    </button>
                  </span>
                )}
              </For>
              <div class="flex-1 flex min-w-[150px] items-center">
                <input
                  type="text"
                  value={newPlaceInput()}
                  onInput={(e) => setNewPlaceInput(e.currentTarget.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      handleAddPlace();
                    }
                  }}
                  placeholder={placesList().length === 0 ? t("infoPanel.editModal.placesPlaceholder") : "+ Lieu (Entrée)..."}
                  class="w-full bg-transparent text-[12px] text-main placeholder:text-subtle-md focus:outline-none px-1"
                />
              </div>
            </div>
          </div>

          {/* Section: Archival Hierarchy (Collection) */}
          <div class="flex flex-col gap-3 p-4 rounded-xl border border-subtle bg-tinted/40">
            <label class="text-[13px] font-semibold text-main select-none">{t("infoPanel.editModal.collectionLabel")}</label>

            {/* List of Existing Hierarchy Levels */}
            <div class="flex flex-col gap-2">
              <Show
                when={collectionList().length > 0}
                fallback={
                  <div class="text-[12px] text-dim italic p-3 border border-dashed border-subtle rounded-lg text-center bg-panel/50 select-none">
                    {t("infoPanel.editModal.noCollection")}
                  </div>
                }
              >
                <For each={collectionList()}>
                  {(col, idx) => (
                    <div class="flex items-center gap-2 p-2 rounded-lg border border-subtle bg-panel shadow-xs">
                      {/* Number badge */}
                      <span class="w-6 h-6 rounded-md bg-tinted border border-subtle flex items-center justify-center text-[11px] font-mono font-semibold text-dim flex-shrink-0 select-none">
                        {idx() + 1}
                      </span>

                      {/* Level text input (direct inline editing) */}
                      <input
                        type="text"
                        value={col}
                        onInput={(e) => handleUpdateCollectionLevel(idx(), e.currentTarget.value)}
                        placeholder="ex : Sous-série 1 E..."
                        class="flex-1 h-8 px-3 rounded-lg border border-subtle bg-tinted text-[13px] text-main font-medium placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all min-w-0"
                      />

                      {/* Move Up */}
                      <button
                        type="button"
                        onClick={() => handleMoveCollectionUp(idx())}
                        disabled={idx() === 0}
                        class="w-7 h-7 rounded-md flex items-center justify-center text-dim hover:text-main hover:bg-hover disabled:opacity-25 disabled:pointer-events-none transition-colors flex-shrink-0 cursor-pointer"
                        title="Monter"
                      >
                        <Icon icon="lucide:arrow-up" width="13" height="13" />
                      </button>

                      {/* Move Down */}
                      <button
                        type="button"
                        onClick={() => handleMoveCollectionDown(idx())}
                        disabled={idx() === collectionList().length - 1}
                        class="w-7 h-7 rounded-md flex items-center justify-center text-dim hover:text-main hover:bg-hover disabled:opacity-25 disabled:pointer-events-none transition-colors flex-shrink-0 cursor-pointer"
                        title="Descendre"
                      >
                        <Icon icon="lucide:arrow-down" width="13" height="13" />
                      </button>

                      {/* Delete */}
                      <button
                        type="button"
                        onClick={() => handleRemoveCollection(idx())}
                        class="w-7 h-7 rounded-md flex items-center justify-center text-dim hover:text-danger hover:bg-danger/10 transition-colors flex-shrink-0 cursor-pointer"
                        title="Supprimer ce niveau"
                      >
                        <Icon icon="lucide:trash-2" width="13" height="13" />
                      </button>
                    </div>
                  )}
                </For>
              </Show>
            </div>

            {/* Add New Hierarchy Level (Separate Row Below) */}
            <div class="flex items-center gap-2 p-2 rounded-lg border border-dashed border-subtle bg-tinted/60">
              <span class="w-6 h-6 rounded-md bg-panel border border-subtle flex items-center justify-center text-[11px] font-mono font-semibold text-dim flex-shrink-0 select-none">
                {collectionList().length + 1}
              </span>

              <input
                type="text"
                value={newCollectionInput()}
                onInput={(e) => setNewCollectionInput(e.currentTarget.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    handleAddCollection();
                  }
                }}
                placeholder={collectionList().length === 0 ? t("infoPanel.editModal.collectionPlaceholder") : "+ Nouveau sous-niveau (Entrée)..."}
                class="flex-1 h-8 px-3 rounded-lg border border-subtle bg-panel text-[13px] text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all min-w-0"
              />

              <Button variant="outline" size="sm" onClick={handleAddCollection} class="flex-shrink-0 h-8 px-3 text-[12px]">
                <Icon icon="lucide:plus" width="13" height="13" />
                <span>{t("infoPanel.editModal.addLevel")}</span>
              </Button>
            </div>
          </div>

          {/* Section: ARK URL */}
          <div class="flex flex-col gap-1.5">
            <label for="reg-ark-url" class="text-[12px] font-medium text-dim select-none">
              {t("infoPanel.editModal.arkLabel")}
            </label>
            <input
              id="reg-ark-url"
              type="url"
              value={arkUrl()}
              onInput={(e) => setArkUrl(e.currentTarget.value)}
              placeholder={t("infoPanel.editModal.arkPlaceholder")}
              class="h-8.5 px-3 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all"
            />
          </div>

          {/* Section: Notes */}
          <div class="flex flex-col gap-1.5">
            <label for="reg-notes" class="text-[12px] font-medium text-dim select-none">
              {t("infoPanel.editModal.notesLabel")}
            </label>
            <textarea
              id="reg-notes"
              rows={3}
              value={notes()}
              onInput={(e) => setNotes(e.currentTarget.value)}
              placeholder={t("infoPanel.editModal.notesPlaceholder")}
              class="px-3 py-2.5 rounded-lg border text-[13px] bg-tinted border-subtle text-main placeholder:text-subtle-md focus:border-accent focus:ring-2 focus:ring-accent/15 focus:outline-none transition-all resize-y min-h-[80px] leading-relaxed"
            />
          </div>
        </form>

        {/* Footer */}
        <div class="flex items-center justify-end gap-3 px-6 py-4 border-t border-subtle bg-tinted flex-shrink-0 select-none">
          <Button variant="outline" size="sm" onClick={props.onClose} disabled={isSaving()}>
            {t("infoPanel.editModal.cancel")}
          </Button>
          <Button variant="primary" size="sm" onClick={handleSubmit} disabled={isSaving()}>
            <Show when={isSaving()} fallback={<Icon icon="lucide:check" width="14" height="14" />}>
              <Icon icon="lucide:loader-2" class="animate-spin" width="14" height="14" />
            </Show>
            {isSaving() ? t("infoPanel.editModal.saving") : t("infoPanel.editModal.save")}
          </Button>
        </div>
      </div>
    </div>
  );
};
