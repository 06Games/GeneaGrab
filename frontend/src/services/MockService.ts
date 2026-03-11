// src/services/MockService.ts
import type { BackendService } from "./api";
import type { EventDetail, RegistryMeta, EventRow, ImageMeta, UserImageMeta, PluginOption } from "../types/registry";
import { 
  MOCK_REGISTRY_DATA, 
  MOCK_IMAGE_META, 
  MOCK_EVENT_ROWS, 
  MOCK_SELECTED_EVENT 
} from "../mocks/registryMocks";

export class MockService implements BackendService {
  // Utility to simulate network delay
  private delay = (ms: number) => new Promise(res => setTimeout(res, ms));

  async getAllRegistries(): Promise<RegistryMeta[]> {
    console.info(`[Mock API] getAllRegistries`);
    await this.delay(300);
    return [MOCK_REGISTRY_DATA];
  }

  async getRegistryMeta(id: string): Promise<RegistryMeta> {
    console.info(`[Mock API] getRegistryMeta: ${id}`);
    await this.delay(300);
    return MOCK_REGISTRY_DATA;
  }
  
  async addRegistry(url: string): Promise<RegistryMeta> {
    console.info(`[Mock API] addRegistry: ${url}`);
    await this.delay(600);
    return MOCK_REGISTRY_DATA;
  }

  async getPluginsForUrl(url: string): Promise<PluginOption[]> {
    console.info(`[Mock API] getPluginsForUrl: ${url}`);
    await this.delay(300);
    return [
      { id: "fs_plugin", name: "FamilySearch Extractor" },
      { id: "gn_plugin", name: "Geneanet Extractor" }
    ];
  }

  async getImageMeta(registryId: string, imageId: number): Promise<ImageMeta> {
    console.info(`[Mock API] getImageMeta: registry ${registryId}, image ${imageId}`);
    await this.delay(200);
    return MOCK_IMAGE_META;
  }

  async saveImageMeta(registryId: string, imageId: number, meta: Partial<UserImageMeta>): Promise<void> {
    console.info(`[Mock API] saveImageMeta: registry ${registryId}, image ${imageId}`, meta);
    await this.delay(500);
  }

  getImageUrl(_registryId: string, _imageId: number, _thumbnail: boolean): string | null {
    return "/src/assets/logo.svg";
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
