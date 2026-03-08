import { createContext, useContext, JSX } from "solid-js";
import type { BackendService } from "../services/api";
import { getBackendService } from "../services/apiFactory";

const BackendContext = createContext<BackendService>();

export function BackendProvider(props: { children: JSX.Element }) {
  const api = getBackendService();

  return (
    <BackendContext.Provider value={api}>
      {props.children}
    </BackendContext.Provider>
  );
}

// Custom hook for components to consume the API
export function useBackend() {
  const context = useContext(BackendContext);
  if (!context) {
    throw new Error("useBackend must be used within a BackendProvider");
  }
  return context;
}
