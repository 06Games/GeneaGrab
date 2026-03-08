// src/services/MockService.ts
import type { BackendService } from "./api";
import type { EventDetail, RegistryMeta, EventRow, ImageMeta } from "../types/registry";
import { 
  MOCK_REGISTRY_DATA, 
  MOCK_IMAGE_META, 
  MOCK_EVENT_ROWS, 
  MOCK_SELECTED_EVENT 
} from "../mocks/registryMocks";

export class MockService implements BackendService {
  // Utility to simulate network delay
  private delay = (ms: number) => new Promise(res => setTimeout(res, ms));

  async getRegistryMeta(id: string): Promise<RegistryMeta> {
    console.info(`[Mock API] getRegistryMeta: ${id}`);
    await this.delay(300);
    return MOCK_REGISTRY_DATA;
  }

  async getImageMeta(registryId: string, imageId: number): Promise<ImageMeta> {
    console.info(`[Mock API] getImageMeta: registry ${registryId}, image ${imageId}`);
    await this.delay(200);
    return MOCK_IMAGE_META;
  }

  async getEventRows(registryId: string): Promise<EventRow[]> {
    console.info(`[Mock API] getEventRows: ${registryId}`);
    await this.delay(400);
    return MOCK_EVENT_ROWS;
  }

  async getEventDetail(eventId: number): Promise<EventDetail | null> {
    console.info(`[Mock API] getEventDetail: ${eventId}`);
    await this.delay(200);
    if (eventId === MOCK_SELECTED_EVENT.event_id) {
      return MOCK_SELECTED_EVENT;
    }
    return null; // Simulate 404 for unknown acts
  }

  async saveAct(event: EventDetail): Promise<void> {
    console.info(`[Mock API] saveAct:`, event);
    await this.delay(600); // Simulate saving time
  }
}
