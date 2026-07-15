import { createSignal, createResource, Show, createEffect } from "solid-js";
import { Button } from "../../ui/primitives";
import { useBackend } from "../../contexts/BackendContext";
import { useI18n } from "../../ui/i18n";
import { Icon } from "@iconify-icon/solid";

export const AddRegistryModal = (props: { onClose: () => void; onAdded: () => void }) => {
  const { t } = useI18n();
  const api = useBackend();
  const [url, setUrl] = createSignal("");
  const [debouncedUrl, setDebouncedUrl] = createSignal("");
  const [selectedProvider, setSelectedProvider] = createSignal("");
  const [isSubmitting, setIsSubmitting] = createSignal(false);

  let timeout: ReturnType<typeof setTimeout>;
  const handleUrlInput = (e: Event) => {
    const val = (e.target as HTMLInputElement).value;
    setUrl(val);
    clearTimeout(timeout);
    timeout = setTimeout(() => setDebouncedUrl(val), 500);
  };

  const [providers] = createResource(debouncedUrl, async (u) => {
    if (!u) return [];
    return api.getProvidersForUrl(u);
  });

  createEffect(() => {
    const list = providers();
    if (list && list.length > 0 && !selectedProvider()) {
      setSelectedProvider(list[0].id);
    }
  });

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    if (!url() || !selectedProvider()) return;

    setIsSubmitting(true);
    try {
      await api.addRegistry(url(), selectedProvider());
      props.onAdded();
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
      <div class="bg-panel w-full max-w-md rounded-xl shadow-lg border border-subtle overflow-hidden flex flex-col">
        <div class="flex items-center justify-between px-5 py-4 border-b border-subtle bg-tinted">
          <h2 class="text-[15px] font-semibold text-main">{t("addModal.title")}</h2>
          <button onClick={props.onClose} class="text-muted hover:text-main focus:outline-none">
            <Icon icon="lucide:x" class="block" />
          </button>
        </div>

        <form onSubmit={handleSubmit} class="p-5 flex flex-col gap-4">
          <div class="flex flex-col gap-1.5">
            <label class="text-[13px] font-medium text-main">{t("addModal.urlLabel")}</label>
            <input
              type="url"
              required
              value={url()}
              onInput={handleUrlInput}
              placeholder={t("addModal.urlPlaceholder")}
              class="w-full px-3 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/20 outline-none transition-all"
            />
          </div>

          <div class="flex flex-col gap-1.5 relative">
            <label class="text-[13px] font-medium text-main">{t("addModal.providerLabel")}</label>
            <select
              required
              value={selectedProvider()}
              onChange={(e) => setSelectedProvider(e.currentTarget.value)}
              disabled={providers.loading || !providers()?.length}
              class="w-full px-3 py-2 rounded-lg border border-subtle bg-tinted text-[13px] text-main focus:border-accent focus:ring-2 focus:ring-accent/20 outline-none transition-all disabled:opacity-50 appearance-none"
            >
              <Show when={providers.loading}>
                <option value="">{t("addModal.providerLoading")}</option>
              </Show>
              <Show when={!providers.loading && providers()?.length === 0}>
                <option value="">{t("addModal.providerNone")}</option>
              </Show>
              <Show when={!providers.loading && providers()?.length !== 0}>
                <option value="" disabled selected={!selectedProvider()}>
                  {t("addModal.providerPlaceholder")}
                </option>
                {providers()?.map((p) => (
                  <option value={p.id}>{p.name}</option>
                ))}
              </Show>
            </select>
            <Icon icon="lucide:chevron-down" class="absolute right-3 bottom-[10px] text-subtle-md w-4 h-4 pointer-events-none" />
          </div>

          <div class="flex justify-end gap-2 mt-2">
            <Button variant="ghost" onClick={props.onClose}>
              {t("addModal.cancel")}
            </Button>
            <Button type="submit" variant="primary" disabled={isSubmitting() || !url() || !selectedProvider()}>
              <Show when={isSubmitting()} fallback={t("addModal.submit")}>
                <Icon icon="lucide:loader-2" class="animate-spin" />
              </Show>
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
