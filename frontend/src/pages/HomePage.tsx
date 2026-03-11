import { createSignal, createResource, createMemo, Show, For, onCleanup } from "solid-js";
import { useBackend } from "../contexts/BackendContext";
import { useI18n } from "../ui/i18n";
import { RegistryCard } from "../components/registry-list/RegistryCard";
import { RegistryFilters } from "../components/registry-list/RegistryFilters";
import { AddRegistryModal } from "../components/registry-list/AddRegistryModal";
import { TopBar } from "../ui/TopBar";
import { Button } from "../ui/primitives";
import { Icon } from "@iconify-icon/solid";

const HomePage = () => {
  const api = useBackend();
  const { t } = useI18n();
  
  const [searchQuery, setSearchQuery] = createSignal("");
  const [selectedType, setSelectedType] = createSignal("");
  const [selectedPlace, setSelectedPlace] = createSignal("");
  const [selectedCollection, setSelectedCollection] = createSignal("");
  const [dateFrom, setDateFrom] = createSignal("");
  const [dateTo, setDateTo] = createSignal("");

  const [isModalOpen, setIsModalOpen] = createSignal(false);
  
  const [page, setPage] = createSignal(1);
  const itemsPerPage = 12;

  // TODO: Replace with paginated/filtered API calls when the backend supports it
  const [registries, { refetch }] = createResource(() => api.getAllRegistries());

  const filteredRegistries = createMemo(() => {
    const list = registries() || [];
    return list.filter(reg => {
      const search = searchQuery().toLowerCase();
      const matchSearch = reg.archive_reference.toLowerCase().includes(search) ||
                          (reg.title && reg.title.toLowerCase().includes(search));
      
      const matchType = selectedType() ? reg.source_types.has(selectedType()) : true;
      const matchPlace = selectedPlace() ? reg.places?.includes(selectedPlace()) : true;
      const matchCol = selectedCollection() ? reg.collection?.includes(selectedCollection()) : true;

      let matchDate = true;
      if (dateFrom() && reg.date_to && reg.date_to < dateFrom()) matchDate = false;
      if (dateTo() && reg.date_from && reg.date_from > dateTo()) matchDate = false;

      return matchSearch && matchType && matchPlace && matchCol && matchDate;
    });
  });

  const paginatedRegistries = createMemo(() => {
    return filteredRegistries().slice(0, page() * itemsPerPage);
  });

  const hasMore = createMemo(() => paginatedRegistries().length < filteredRegistries().length);

  createMemo(() => {
    // Reset pagination when filters change
    searchQuery();
    selectedType();
    selectedPlace();
    selectedCollection();
    dateFrom();
    dateTo();
    setPage(1);
  });

  return (
    <div class="min-h-screen bg-app text-main flex flex-col antialiased" style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}>
      <TopBar 
        breadcrumbs={[t("home.title")]}
        right={
          <Button variant="primary" onClick={() => setIsModalOpen(true)}>
            <Icon icon="lucide:plus" class="w-4 h-4" /> {t("home.addRegistry")}
          </Button>
        }
      />

      <main class="flex-1 max-w-6xl w-full mx-auto px-6 py-8 flex flex-col gap-6">
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

        <Show when={registries.loading}>
          <div class="py-16 flex justify-center text-dim">
            <Icon icon="lucide:loader-2" class="w-8 h-8 animate-spin" />
          </div>
        </Show>

        <Show when={!registries.loading && filteredRegistries().length === 0}>
          <div class="py-16 flex flex-col items-center justify-center text-dim border-2 border-dashed border-subtle rounded-xl">
            <Icon icon="lucide:folder-search" class="w-12 h-12 mb-3 text-subtle-md" />
            <p>{t("home.noResults")}</p>
          </div>
        </Show>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          <For each={paginatedRegistries()}>
            {(registry) => <RegistryCard registry={registry} />}
          </For>
        </div>

        <Show when={hasMore()}>
          <div 
            ref={(el) => {
              const observer = new IntersectionObserver((entries) => {
                if (entries[0].isIntersecting) setPage(p => p + 1);
              }, { rootMargin: "100px" });
              observer.observe(el);
              onCleanup(() => observer.disconnect());
            }} 
            class="h-10 flex items-center justify-center text-dim mt-4"
          >
            <Icon icon="lucide:loader-2" class="w-6 h-6 animate-spin" />
          </div>
        </Show>
      </main>

      <Show when={isModalOpen()}>
        <AddRegistryModal
          onClose={() => setIsModalOpen(false)}
          onAdded={() => {
            setIsModalOpen(false);
            refetch();
          }}
        />
      </Show>
    </div>
  );
};

export default HomePage;
