import { invoke } from "@tauri-apps/api/core";
import { RegistryMeta, EventRow, EventDetail, ImageMeta, UserImageMeta } from "../types/registry";
import { BackendService } from "./api";

export class TauriService implements BackendService {
    async getRegistryMeta(id: string): Promise<RegistryMeta> {
        const res = await invoke<any>("get_registry_meta", { id });
        
        if (Array.isArray(res.source_types)) {
            res.source_types = new Set(res.source_types);
        }
        return res as RegistryMeta;
    }

    async getImageMeta(registryId: string, imageId: number): Promise<ImageMeta> {
        const res = await invoke<any>("get_image_meta", { registryId, imageId });
        if (typeof res.act_types === "object" && res.act_types !== null)
            res.act_types = new Map(Object.entries(res.act_types));
        else res.act_types = new Map();
        return res as ImageMeta;
    }

    async saveImageMeta(registryId: string, imageId: number, meta: Partial<UserImageMeta>): Promise<void> {
        await invoke("save_image_meta", { registryId, imageId, meta });
    }

    getImageUrl(registryId: string, imageId: number, thumbnail: boolean): string | null {
        return `tiles://localhost/${registryId}/${imageId}/${thumbnail}`;
    }

    async getEventRows(registryId: string): Promise<EventRow[]> {
        return await invoke<EventRow[]>("get_event_rows", { registryId });
    }

    async getEventDetail(eventId: number): Promise<EventDetail | null> {
        return await invoke<EventDetail | null>("get_event_detail", { eventId });
    }

    async saveAct(event: EventDetail): Promise<void> {
        await invoke("save_act", { event });
    }
}
