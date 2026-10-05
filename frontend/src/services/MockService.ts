import { MOCK_EVENT_ROWS, MOCK_IMAGE_META, MOCK_REGISTRY_DATA, MOCK_SELECTED_EVENT } from "../mocks/registryMocks";
import { EventDetail, EventRow } from "../types";
import { CursorPayload, CursorResponse } from "../types/cursor_requests";
import { ImageMeta, UserImageMeta } from "../types/image";
import {
  LocationGroupMeta,
  ProviderOption,
  RegistryFilters,
  RegistryMeta,
} from "../types/registry";
import type { BackendService } from "./api";

export class MockService implements BackendService {
  private delay = (ms: number) => new Promise((res) => setTimeout(res, ms));

  private getMockRegistries(): RegistryMeta[] {
    return [
      {
        ...MOCK_REGISTRY_DATA,
        id: 1,
        title: "Registres de Brignoles",
        places: [["France", "Var", "Brignoles"]],
      },
      {
        ...MOCK_REGISTRY_DATA,
        id: 2,
        title: "Registres de Lantosque (Loda)",
        places: [["France", "Alpes-Maritimes", "Lantosque", "Loda"]],
      },
      {
        ...MOCK_REGISTRY_DATA,
        id: 3,
        title: "Registres paroissiaux de Nice",
        places: [["France", "Alpes-Maritimes", "Nice"]],
      },
      {
        ...MOCK_REGISTRY_DATA,
        id: 4,
        title: "Registres de Lantosque & Nice",
        places: [
          ["France", "Alpes-Maritimes", "Lantosque"],
          ["France", "Alpes-Maritimes", "Nice"],
        ],
      },
      {
        ...MOCK_REGISTRY_DATA,
        id: 5,
        title: "Registres sans lieu",
        places: [],
      },
    ];
  }

  private filterRegistries(data: RegistryMeta[], filters?: RegistryFilters): RegistryMeta[] {
    let filtered = data;
    if (filters?.search_term) {
      const term = filters.search_term.toLowerCase();
      filtered = filtered.filter(
        (d) =>
          d.archive_reference.toLowerCase().includes(term) ||
          (d.title && d.title.toLowerCase().includes(term))
      );
    }
    if (filters?.place) {
      const p = filters.place.toLowerCase();
      filtered = filtered.filter((d) =>
        d.places.some((parts) => parts.some((part) => part.toLowerCase().includes(p)))
      );
    }
    if (filters?.is_unknown_location || (filters?.location && filters.location.length === 0)) {
      filtered = filtered.filter((d) => !d.places || d.places.length === 0);
    } else if (filters?.location && filters.location.length > 0) {
      const locKey = filters.location.join(" > ").toLowerCase();
      filtered = filtered.filter((d) =>
        d.places?.some((p) => p.join(" > ").toLowerCase() === locKey)
      );
    }
    return filtered;
  }

  getRegistries = async (payload: CursorPayload<RegistryFilters>): Promise<CursorResponse<RegistryMeta>> => {
    console.info(`[Mock API] getRegistries`, payload);
    await this.delay(300);

    const mockData = this.getMockRegistries();
    const filtered = this.filterRegistries(mockData, payload.filters ?? undefined);

    const cursorIndex = payload.cursor ? filtered.findIndex((d) => d.id === payload.cursor) : -1;
    const start = cursorIndex >= 0 ? cursorIndex + 1 : 0;
    const paginated = filtered.slice(start, start + payload.limit);

    const hasMore = start + payload.limit < filtered.length;
    const nextCursor = hasMore ? paginated[paginated.length - 1]?.id : null;

    return { data: paginated, next_cursor: nextCursor ?? null };
  };

  getLocationGroups = async (filters?: RegistryFilters): Promise<LocationGroupMeta[]> => {
    console.info(`[Mock API] getLocationGroups`, filters);
    await this.delay(200);

    const mockData = this.getMockRegistries();
    const filtered = this.filterRegistries(mockData, filters);

    const groupMap = new Map<string, { location: string[]; displayName: string; count: number }>();

    for (const item of filtered) {
      if (!item.places || item.places.length === 0) {
        let g = groupMap.get("__unknown__");
        if (!g) {
          g = { location: [], displayName: "", count: 0 };
          groupMap.set("__unknown__", g);
        }
        g.count += 1;
      } else {
        for (const place of item.places) {
          const key = place.join(" > ").toLowerCase();
          let g = groupMap.get(key);
          if (!g) {
            g = { location: place, displayName: place.join(" > "), count: 0 };
            groupMap.set(key, g);
          }
          g.count += 1;
        }
      }
    }

    const groups: LocationGroupMeta[] = Array.from(groupMap.entries()).map(([key, val]) => ({
      key,
      location: val.location,
      display_name: val.displayName,
      count: val.count,
    }));

    groups.sort((a, b) => {
      const aUnknown = a.location.length === 0 || a.key === "__unknown__";
      const bUnknown = b.location.length === 0 || b.key === "__unknown__";
      if (aUnknown && bUnknown) return 0;
      if (aUnknown) return 1;
      if (bUnknown) return -1;

      const len = Math.min(a.location.length, b.location.length);
      for (let i = 0; i < len; i++) {
        const cmp = a.location[i].localeCompare(b.location[i], undefined, { sensitivity: "base" });
        if (cmp !== 0) return cmp;
      }
      return a.location.length - b.location.length;
    });

    return groups;
  };


  getRegistryMeta = async (id: number): Promise<RegistryMeta> => {
    console.info(`[Mock API] getRegistryMeta: ${id}`);
    await this.delay(300);
    return MOCK_REGISTRY_DATA;
  };

  saveRegistryMeta = async (id: number, meta: Partial<UserRegistryMeta>): Promise<void> => {
    console.info(`[Mock API] saveRegistryMeta: registry ${id}`, meta);
    await this.delay(400);
    const sourceTypes = meta.source_types
      ? meta.source_types instanceof Set
        ? meta.source_types
        : new Set(meta.source_types)
      : MOCK_REGISTRY_DATA.source_types;

    Object.assign(MOCK_REGISTRY_DATA, {
      ...meta,
      source_types: sourceTypes,
    });
  };

  addRegistry = async (url: string, providerId: string): Promise<RegistryMeta> => {
    console.info(`[Mock API] addRegistry: ${url}, provider: ${providerId}`);
    await this.delay(600);
    return MOCK_REGISTRY_DATA;
  };

  deleteRegistry = async (id: number): Promise<void> => {
    console.info(`[Mock API] deleteRegistry: ${id}`);
    await this.delay(300);
  };

  getProvidersForUrl = async (url: string): Promise<ProviderOption[]> => {
    console.info(`[Mock API] getProvidersForUrl: ${url}`);
    await this.delay(300);
    return [
      { id: "fs_provider", name: "FamilySearch Extractor" },
      { id: "gn_provider", name: "Geneanet Extractor" },
    ];
  };

  getAvailablePlaces = async (): Promise<string[]> => {
    await this.delay(200);
    return ["Brignoles", "Toulon", "Draguignan", "Nice", "Antibes", "Cannes"];
  };

  getAvailableCollections = async (): Promise<string[]> => {
    await this.delay(200);
    return ["État civil", "Registres paroissiaux", "Recensements", "Minutes notariales", "Registres matricules"];
  };

  getImageMeta = async (registryId: number, imageId: number): Promise<ImageMeta> => {
    console.info(`[Mock API] getImageMeta: registry ${registryId}, image ${imageId}`);
    await this.delay(200);
    return MOCK_IMAGE_META;
  };

  saveImageMeta = async (registryId: number, imageId: number, meta: Partial<UserImageMeta>): Promise<void> => {
    console.info(`[Mock API] saveImageMeta: registry ${registryId}, image ${imageId}`, meta);
    await this.delay(500);
  };

  getTileUrl = (_registryId: number, _imageId: number, _level: number, _x: number, _y: number): string | null => {
    return "/src/assets/logo.svg";
  };

  getImageUrl = (_registryId: number, _imageId: number): string | null => {
    return "/src/assets/logo.svg";
  };

  downloadImage = async (registryId: number, imageId: number): Promise<void> => {
    console.info(`[Mock API] downloadImage: registry ${registryId}, image ${imageId}`);
    await this.delay(1500);
  };

  onDownloadProgress = async (_registryId: number, _imageId: number, cb: (current: number, total: number) => void): Promise<() => void> => {
    let current = 0;
    const total = 10;
    const interval = setInterval(() => {
      current += 1;
      cb(current, total);
    }, 150);
    return () => clearInterval(interval);
  };

  getEventRows = async (registryId: number): Promise<EventRow[]> => {
    console.info(`[Mock API] getEventRows: ${registryId}`);
    await this.delay(400);
    return MOCK_EVENT_ROWS;
  };

  getEventDetail = async (eventId: number): Promise<EventDetail | null> => {
    console.info(`[Mock API] getEventDetail: ${eventId}`);
    await this.delay(200);
    if (eventId === MOCK_SELECTED_EVENT.event_id) {
      return MOCK_SELECTED_EVENT;
    }
    return null;
  };

  saveAct = async (event: EventDetail): Promise<void> => {
    console.info(`[Mock API] saveAct:`, event);
    await this.delay(600);
  };
}
