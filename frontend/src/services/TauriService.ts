import { invoke } from "@tauri-apps/api/core";
import { RegistryMeta, EventRow, EventDetail, ImageMeta, UserImageMeta, PluginOption } from "../types/registry";
import { BackendService } from "./api";

export class TauriService implements BackendService {

    async getAllRegistries(): Promise<RegistryMeta[]> {
        const res = await invoke<any[]>("get_all_registries");
        return res.map(r => {
            if (Array.isArray(r.source_types)) {
                r.source_types = new Set(r.source_types);
            }
            return r as RegistryMeta;
        });
    }
    
    async getRegistryMeta(id: string): Promise<RegistryMeta> {
        const res = await invoke<any>("get_registry", { id });
        
        if (Array.isArray(res.source_types)) {
            res.source_types = new Set(res.source_types);
        }
        return res as RegistryMeta;
    }

    async addRegistry(url: string): Promise<RegistryMeta> {
        const res = await invoke<any>("add_registry", { url });
        if (Array.isArray(res.source_types)) {
            res.source_types = new Set(res.source_types);
        }
        return res as RegistryMeta;
    }

    async getPluginsForUrl(_url: string): Promise<PluginOption[]> {
        // TODO: Implement get_plugins_for_url command in Rust backend
        console.warn("getPluginsForUrl not implemented in Tauri backend yet, returning mock data.");
        return [
          { id: "default", name: "Default Extractor" }
        ];
    }

    async getAvailablePlaces(): Promise<string[]> {
        // TODO: Implement backend extraction of unique places
        console.warn("getAvailablePlaces not implemented");
        return [];
    }

    async getAvailableCollections(): Promise<string[]> {
        // TODO: Implement backend extraction of unique collections
        console.warn("getAvailableCollections not implemented");
        return [];
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
