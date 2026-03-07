import { useNavigate, useParams } from "@solidjs/router";
import { RegistryViewer } from "../components/registry-viewer/RegistryViewer";
import { 
  MOCK_REGISTRY_DATA, 
  MOCK_IMAGE_META, 
  MOCK_EVENT_ROWS, 
  MOCK_SELECTED_EVENT 
} from "../mocks/registryMocks";
import type { EventDetail } from "../types/registry";

const ViewerPage = () => {
  const params = useParams();
  const navigate = useNavigate();
  
  const handleImageChange = (newImageId: number) => {
    navigate(`/registry/${params.id}/${newImageId}`, { replace: true });
  };

  const handleGetEventDetail = (id: number): EventDetail | null => {
    // Simulating an API lookup or a store fetch
    if (id === MOCK_SELECTED_EVENT.event_id) {
      return MOCK_SELECTED_EVENT;
    }
    return null;
  };

  return (
    <RegistryViewer 
      registryMeta={MOCK_REGISTRY_DATA}
      imageMeta={MOCK_IMAGE_META}
      eventRows={MOCK_EVENT_ROWS}
      totalImages={348}
      initialImage={params.imageId ? parseInt(params.imageId) : undefined}
      onImageChange={handleImageChange}
      getEventDetail={handleGetEventDetail}
      onSaveAct={(event) => console.log(`ViewerPage received save request for registry ${params.id}:`, event)}
      onValidateAndNext={(event) => console.log(`ViewerPage received validate request for registry ${params.id}:`, event)}
    />
  );
};

export default ViewerPage;
