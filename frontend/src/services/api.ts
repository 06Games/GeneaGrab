import type { EventDetail, RegistryMeta, EventRow, ImageMeta, UserImageMeta } from "../types/registry";

export interface BackendService {
    // Registry
    getRegistryMeta(id: string): Promise<RegistryMeta>;
    getImageMeta(registryId: string, imageId: number): Promise<ImageMeta>;
    saveImageMeta(registryId: string, imageId: number, meta: Partial<UserImageMeta>): Promise<void>;

    // Index
    getEventRows(registryId: string): Promise<EventRow[]>;
    getEventDetail(eventId: number): Promise<EventDetail | null>;
    saveAct(event: EventDetail): Promise<void>;
}
