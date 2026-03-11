import type { EventDetail, RegistryMeta, EventRow, ImageMeta, UserImageMeta, PluginOption, CursorPayload, CursorResponse, RegistryFilters } from "../types/registry";

export interface BackendService {
    // Registry
    getAllRegistries(payload: CursorPayload<RegistryFilters>): Promise<CursorResponse<RegistryMeta>>;
    getRegistryMeta(id: number): Promise<RegistryMeta>;
    addRegistry(url: string): Promise<RegistryMeta>;
    getPluginsForUrl(url: string): Promise<PluginOption[]>;
    
    // Filters metadata
    getAvailablePlaces(): Promise<string[]>;
    getAvailableCollections(): Promise<string[]>;

    // Images
    getImageMeta(registryId: number, imageId: number): Promise<ImageMeta>;
    saveImageMeta(registryId: number, imageId: number, meta: Partial<UserImageMeta>): Promise<void>;
    getImageUrl(registryId: number, imageId: number, thumbnail: boolean): string | null;

    // Index
    getEventRows(registryId: number): Promise<EventRow[]>;
    getEventDetail(eventId: number): Promise<EventDetail | null>;
    saveAct(event: EventDetail): Promise<void>;
}
