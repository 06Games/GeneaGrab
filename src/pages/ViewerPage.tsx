// src/pages/ViewerPage.tsx
import { useParams, useNavigate } from "@solidjs/router";
import { createResource, Show, createSignal } from "solid-js";
import { RegistryViewer } from "../components/registry-viewer/RegistryViewer";
import { useBackend } from "../contexts/BackendContext";
import type { EventDetail } from "../types/registry";

const ViewerPage = () => {
  const params = useParams();
  const navigate = useNavigate();
  const api = useBackend();

  const [currentImageId, setCurrentImageId] = createSignal(
    params.imageId ? parseInt(params.imageId, 10) : 1
  );

  const [registryMeta] = createResource(() => params.id, (id) => api.getRegistryMeta(id));
  const [eventRows] = createResource(() => params.id, (id) => api.getEventRows(id));
  const [imageMeta] = createResource(
    () => ({ regId: params.id!, imgId: currentImageId() }),
    ({ regId, imgId }) => api.getImageMeta(regId, imgId)
  );

  const [detailCache, setDetailCache] = createSignal<Record<number, EventDetail>>({});
  
  // Track which IDs are currently being fetched to prevent duplicates
  const fetchingIds = new Set<number>();

  const handleImageChange = (newImageId: number) => {
    setCurrentImageId(newImageId);
    navigate(`/registry/${params.id}/${newImageId}`, { replace: true });
  };

  const handleGetEventDetail = (id: number): EventDetail | null => {
    const cached = detailCache()[id];
    if (cached) return cached; // Return immediately if we already have it

    // If we aren't already fetching this ID, go get it
    if (!fetchingIds.has(id)) {
      fetchingIds.add(id);
      
      setTimeout(() => {
        api.getEventDetail(id).then(detail => {
          if (detail) {
            setDetailCache(prev => ({ ...prev, [id]: detail }));
          }
        }).catch(err => {
          console.error("Failed to fetch detail:", err);
          fetchingIds.delete(id); // Allow retrying on failure
        });
      }, 0);
    }

    return null; // Return null while loading
  };

  const handleSaveAct = async (event: EventDetail) => {
    await api.saveAct(event);
    console.log(`Successfully saved event ${event.event_id} to backend.`);
  };

  return (
    <div class="w-screen h-screen bg-app">
      <Show 
        when={registryMeta() && imageMeta() && eventRows()} 
        fallback={<div class="flex items-center justify-center w-full h-full text-dim">Loading registry data...</div>}
      >
        <RegistryViewer 
          registryMeta={registryMeta()!}
          imageMeta={imageMeta()!}
          eventRows={eventRows()!}
          initialImage={currentImageId()}
          onImageChange={handleImageChange}
          getEventDetail={handleGetEventDetail}
          onSaveAct={handleSaveAct}
          onValidateAndNext={handleSaveAct}
        />
      </Show>
    </div>
  );
};

export default ViewerPage;
