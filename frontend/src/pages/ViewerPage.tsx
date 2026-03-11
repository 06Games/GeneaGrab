import { useParams } from "@solidjs/router";
import { RegistryViewer } from "../components/registry-viewer/RegistryViewer";

const ViewerPage = () => {
  const params = useParams();

  const registryId = parseInt(params.id!, 10);
  const initialImageId = params.imageId ? parseInt(params.imageId, 10) : 1;

  return (
    <div class="w-screen h-screen bg-app">
      <RegistryViewer 
        registryId={registryId} 
        initialImageId={initialImageId} 
      />
    </div>
  );
};

export default ViewerPage;
