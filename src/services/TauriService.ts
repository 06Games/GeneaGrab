import { RegistryMeta, EventRow, EventDetail, ImageMeta, UserImageMeta } from "../types/registry";
import { BackendService } from "./api";

export class TauriService implements BackendService {
    getRegistryMeta(id: string): Promise<RegistryMeta> {
        throw new Error("Method not implemented.");
    }
    getImageMeta(registryId: string, imageId: number): Promise<ImageMeta> {
        throw new Error("Method not implemented.");
    }
    saveImageMeta(registryId: string, imageId: number, meta: Partial<UserImageMeta>): Promise<void> {
        throw new Error("Method not implemented.");
    }

    getEventRows(registryId: string): Promise<EventRow[]> {
        throw new Error("Method not implemented.");
    }
    getEventDetail(eventId: number): Promise<EventDetail | null> {
        throw new Error("Method not implemented.");
    }
    saveAct(event: EventDetail): Promise<void> {
        throw new Error("Method not implemented.");
    }
}
