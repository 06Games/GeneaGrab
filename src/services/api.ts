import type { EventDetail, RegistryMeta, EventRow, ImageMeta } from "../types/registry";

export interface BackendService {
    getRegistryMeta(id: string): Promise<RegistryMeta>;
    getImageMeta(registryId: string, imageId: number): Promise<ImageMeta>;
    getEventRows(registryId: string): Promise<EventRow[]>;
    getEventDetail(eventId: number): Promise<EventDetail | null>;
    saveAct(event: EventDetail): Promise<void>;
}
