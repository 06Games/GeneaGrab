import { createContext, useContext, JSX } from "solid-js";
import { EventDetail } from "../types";
import { UserImageMeta } from "../types/image";

export interface RegistryActions {
  onSaveImageMeta?: (meta: Partial<UserImageMeta>) => Promise<void>;
  onSaveAct?: (event: EventDetail) => void;
  onValidateAndNext?: (event: EventDetail) => void;
  onNewAct?: () => void;
  onReset?: () => void;
}

const RegistryActionsContext = createContext<RegistryActions>({});

export function RegistryActionsProvider(props: RegistryActions & { children: JSX.Element }) {
  return <RegistryActionsContext.Provider value={props}>{props.children}</RegistryActionsContext.Provider>;
}

export function useRegistryActions() {
  return useContext(RegistryActionsContext);
}
