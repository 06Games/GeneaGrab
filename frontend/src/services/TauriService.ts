import { invoke } from "@tauri-apps/api/core";
import { EventDetail, EventRow } from "../types";
import { CursorPayload, CursorResponse } from "../types/cursor_requests";
import { ImageMeta, UserImageMeta } from "../types/image";
import { PluginOption, RegistryFilters, RegistryMeta } from "../types/registry";
import { BackendService } from "./api";

export class TauriService implements BackendService {

    async getAllRegistries(payload: CursorPayload<RegistryFilters>): Promise<CursorResponse<RegistryMeta>> {
        const res = await invoke<any>("get_all_registries", { payload });
        return {
            data: res.data.map((r: any) => {
                if (Array.isArray(r.source_types)) {
                    r.source_types = new Set(r.source_types);
                }
                return r as RegistryMeta;
            }),
            next_cursor: res.next_cursor
        };
    }

    async getRegistryMeta(id: number): Promise<RegistryMeta> {
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

    async getImageMeta(registryId: number, imageId: number): Promise<ImageMeta> {
        const res = await invoke<any>("get_image_meta", { registryId, imageId });
        if (typeof res.act_types === "object" && res.act_types !== null)
            res.act_types = new Map(Object.entries(res.act_types));
        else res.act_types = new Map();
        return res as ImageMeta;
    }

    async saveImageMeta(registryId: number, imageId: number, meta: Partial<UserImageMeta>): Promise<void> {
        await invoke("save_image_meta", { registryId, imageId, meta });
    }

    getImageUrl(registryId: number, imageId: number, thumbnail: boolean): string | null {
        return `tiles://localhost/${registryId}/${imageId}/${thumbnail}`;
    }

    async getEventRows(registryId: number): Promise<EventRow[]> {
        return await invoke<EventRow[]>("get_event_rows", { registryId });
    }

    async getEventDetail(eventId: number): Promise<EventDetail | null> {
        return await invoke<EventDetail | null>("get_event_detail", { eventId });
    }

    async saveAct(event: EventDetail): Promise<void> {
        await invoke("save_act", { event });
    }
}
