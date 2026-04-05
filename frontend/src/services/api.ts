import { EventDetail, EventRow } from "../types";
import { CursorPayload, CursorResponse } from "../types/cursor_requests";
import { ImageMeta, UserImageMeta } from "../types/image";
import { PluginOption, RegistryFilters, RegistryMeta } from "../types/registry";

export interface BackendService {
    // Registry
    getAllRegistries(payload: CursorPayload<RegistryFilters>): Promise<CursorResponse<RegistryMeta>>;
    getRegistryMeta(id: number): Promise<RegistryMeta>;
    addRegistry(url: string, pluginId: string): Promise<RegistryMeta>;
    getPluginsForUrl(url: string): Promise<PluginOption[]>;

    // Filters metadata
    getAvailablePlaces(): Promise<string[]>;
    getAvailableCollections(): Promise<string[]>;

    // Images
    getImageMeta(registryId: number, imageId: number): Promise<ImageMeta>;
    saveImageMeta(registryId: number, imageId: number, meta: Partial<UserImageMeta>): Promise<void>;
    getTileUrl(registryId: number, imageId: number, level: number, x: number, y: number): string | null;
    getImageUrl(registryId: number, imageId: number): string | null;

    // Index
    getEventRows(registryId: number): Promise<EventRow[]>;
    getEventDetail(eventId: number): Promise<EventDetail | null>;
    saveAct(event: EventDetail): Promise<void>;
}
