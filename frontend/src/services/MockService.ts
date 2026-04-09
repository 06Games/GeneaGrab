import {
  MOCK_EVENT_ROWS,
  MOCK_IMAGE_META,
  MOCK_REGISTRY_DATA,
  MOCK_SELECTED_EVENT
} from "../mocks/registryMocks";
import { EventDetail, EventRow } from "../types";
import { CursorPayload, CursorResponse } from "../types/cursor_requests";
import { ImageMeta, UserImageMeta } from "../types/image";
import { PluginOption, RegistryFilters, RegistryMeta } from "../types/registry";
import type { BackendService } from "./api";

export class MockService implements BackendService {
  private delay = (ms: number) => new Promise(res => setTimeout(res, ms));

  getAllRegistries = async (payload: CursorPayload<RegistryFilters>): Promise<CursorResponse<RegistryMeta>> => {
    console.info(`[Mock API] getAllRegistries`, payload);
    await this.delay(600);

    // Generate mock data for pagination
    let data: RegistryMeta[] = [MOCK_REGISTRY_DATA];
    for (let i = 1; i <= 60; i++) {
      data.push({
        ...MOCK_REGISTRY_DATA,
        id: i,
        archive_reference: `5 Mi 1/${100 + i}`,
        title: `Mock Registry Part ${i}`
      });
    }

    // Apply simple filters for mock
    if (payload.filters) {
      if (payload.filters.search_term) {
        const term = payload.filters.search_term.toLowerCase();
        data = data.filter(d =>
          d.archive_reference.toLowerCase().includes(term) ||
          (d.title && d.title.toLowerCase().includes(term))
        );
      }
    }

    // Perform offset logic to simulate cursor
    const cursorIndex = payload.cursor ? data.findIndex(d => d.id === payload.cursor) : -1;
    const start = cursorIndex >= 0 ? cursorIndex + 1 : 0;
    const paginated = data.slice(start, start + payload.limit);

    // Resolve next cursor
    const hasMore = start + payload.limit < data.length;
    const nextCursor = hasMore ? paginated[paginated.length - 1]?.id : null;

    return { data: paginated, next_cursor: nextCursor ?? null };
  }

  getRegistryMeta = async (id: number): Promise<RegistryMeta> => {
    console.info(`[Mock API] getRegistryMeta: ${id}`);
    await this.delay(300);
    return MOCK_REGISTRY_DATA;
  }

  addRegistry = async (url: string, pluginId: string): Promise<RegistryMeta> => {
    console.info(`[Mock API] addRegistry: ${url}, plugin: ${pluginId}`);
    await this.delay(600);
    return MOCK_REGISTRY_DATA;
  }

  getPluginsForUrl = async (url: string): Promise<PluginOption[]> => {
    console.info(`[Mock API] getPluginsForUrl: ${url}`);
    await this.delay(300);
    return [
      { id: "fs_plugin", name: "FamilySearch Extractor" },
      { id: "gn_plugin", name: "Geneanet Extractor" }
    ];
  }

  getAvailablePlaces = async (): Promise<string[]> => {
    await this.delay(200);
    return ["Brignoles", "Toulon", "Draguignan", "Nice", "Antibes", "Cannes"];
  }

  getAvailableCollections = async (): Promise<string[]> => {
    await this.delay(200);
    return ["État civil", "Registres paroissiaux", "Recensements", "Minutes notariales", "Registres matricules"];
  }

  getImageMeta = async (registryId: number, imageId: number): Promise<ImageMeta> => {
    console.info(`[Mock API] getImageMeta: registry ${registryId}, image ${imageId}`);
    await this.delay(200);
    return MOCK_IMAGE_META;
  }

  saveImageMeta = async (registryId: number, imageId: number, meta: Partial<UserImageMeta>): Promise<void> => {
    console.info(`[Mock API] saveImageMeta: registry ${registryId}, image ${imageId}`, meta);
    await this.delay(500);
  }

  getTileUrl = (_registryId: number, _imageId: number, _level: number, _x: number, _y: number): string | null => {
    return "/src/assets/logo.svg";
  }

  getImageUrl = (_registryId: number, _imageId: number): string | null => {
    return "/src/assets/logo.svg";
  }

  onDownloadProgress = async (_registryId: number, _imageId: number, cb: (current: number, total: number) => void): Promise<() => void> => {
    let current = 0;
    const total = 10;
    const interval = setInterval(() => {
      current += 1;
      cb(current, total);
    }, 150);
    return () => clearInterval(interval);
  }

  getEventRows = async (registryId: number): Promise<EventRow[]> => {
    console.info(`[Mock API] getEventRows: ${registryId}`);
    await this.delay(400);
    return MOCK_EVENT_ROWS;
  }

  getEventDetail = async (eventId: number): Promise<EventDetail | null> => {
    console.info(`[Mock API] getEventDetail: ${eventId}`);
    await this.delay(200);
    if (eventId === MOCK_SELECTED_EVENT.event_id) {
      return MOCK_SELECTED_EVENT;
    }
    return null;
  }

  saveAct = async (event: EventDetail): Promise<void> => {
    console.info(`[Mock API] saveAct:`, event);
    await this.delay(600);
  }
}
