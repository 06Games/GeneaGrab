import { MockService } from "./MockService";
import { TauriService } from "./TauriService";
import type { BackendService } from "./api";

export function getBackendService(): BackendService {
  const isTauri = typeof window !== 'undefined' && ("__TAURI_INTERNALS__" in window || "__TAURI__" in window);
  
  if (isTauri) {
    return new TauriService();
  }

  return new MockService();
}

export const api = getBackendService();
