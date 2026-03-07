import { createContext, useContext, JSX } from "solid-js";
import type { EventDetail } from "../types/registry";

export interface RegistryActions {
  onSaveAct?: (event: EventDetail) => void;
  onValidateAndNext?: (event: EventDetail) => void;
  onNewAct?: () => void;
  onReset?: () => void;
}

const RegistryActionsContext = createContext<RegistryActions>({});

export function RegistryActionsProvider(props: RegistryActions & { children: JSX.Element }) {
  return (
    <RegistryActionsContext.Provider value={props}>
      {props.children}
    </RegistryActionsContext.Provider>
  );
}

export function useRegistryActions() {
  return useContext(RegistryActionsContext);
}
