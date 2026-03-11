import type { EventDetail, RegistryMeta, EventRow, ImageMeta, UserImageMeta } from "../types/registry";

export interface BackendService {
    // Registry
    getAllRegistries(): Promise<RegistryMeta[]>;
    getRegistryMeta(id: string): Promise<RegistryMeta>;
    addRegistry(url: string): Promise<RegistryMeta>;
    getImageMeta(registryId: string, imageId: number): Promise<ImageMeta>;
    saveImageMeta(registryId: string, imageId: number, meta: Partial<UserImageMeta>): Promise<void>;
    getImageUrl(registryId: string, imageId: number, thumbnail: boolean): string | null;

    // Index
    getEventRows(registryId: string): Promise<EventRow[]>;
    getEventDetail(eventId: number): Promise<EventDetail | null>;
    saveAct(event: EventDetail): Promise<void>;
}
