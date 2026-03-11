import { createSignal, createResource, createMemo, Show, For } from "solid-js";
import { useBackend } from "../contexts/BackendContext";
import { useI18n } from "../ui/i18n";
import { RegistryCard } from "../components/registry-list/RegistryCard";
import { RegistryFilters } from "../components/registry-list/RegistryFilters";
import { AddRegistryModal } from "../components/registry-list/AddRegistryModal";
import { Button } from "../ui/primitives";
import { Icon } from "@iconify-icon/solid";

const HomePage = () => {
  const api = useBackend();
  const { t } = useI18n();
  
  const [searchQuery, setSearchQuery] = createSignal("");
  const [selectedType, setSelectedType] = createSignal("");
  const [isModalOpen, setIsModalOpen] = createSignal(false);

  // TODO: Replace with paginated/filtered API calls when the backend supports it
  const [registries, { refetch }] = createResource(() => api.getAllRegistries());

  const filteredRegistries = createMemo(() => {
    const list = registries() || [];
    return list.filter(reg => {
      const search = searchQuery().toLowerCase();
      const matchSearch = reg.archive_reference.toLowerCase().includes(search) ||
                          reg.town.toLowerCase().includes(search);
      const matchType = selectedType() ? reg.source_types.has(selectedType()) : true;
      return matchSearch && matchType;
    });
  });

  return (
    <div class="min-h-screen bg-app text-main flex flex-col antialiased" style={{ "font-family": "'Outfit', 'Helvetica Neue', system-ui, sans-serif" }}>
      <header class="bg-panel border-b border-subtle px-6 py-4 flex items-center justify-between sticky top-0 z-10 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-accent text-white flex items-center justify-center font-bold text-lg shadow-inner">
            G
          </div>
          <h1 class="text-xl font-bold text-main">{t("home.title")}</h1>
        </div>
        <Button variant="primary" onClick={() => setIsModalOpen(true)}>
          <Icon icon="lucide:plus" class="w-4 h-4" /> {t("home.addRegistry")}
        </Button>
      </header>

      <main class="flex-1 max-w-6xl w-full mx-auto px-6 py-8 flex flex-col gap-6">
        <RegistryFilters
          searchQuery={searchQuery()}
          onSearchChange={setSearchQuery}
          selectedType={selectedType()}
          onTypeChange={setSelectedType}
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
          <For each={filteredRegistries()}>
            {(registry) => <RegistryCard registry={registry} />}
          </For>
        </div>

        <Show when={filteredRegistries().length > 0}>
          <div class="flex justify-center mt-6">
            <Button variant="outline">{t("home.loadMore")}</Button>
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
