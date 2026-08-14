import { invoke } from "@tauri-apps/api/core";
import { listen } from "@tauri-apps/api/event";
import { EventDetail, EventRow } from "../types";
import { CursorPayload, CursorResponse } from "../types/cursor_requests";
import { ImageMeta, UserImageMeta } from "../types/image";
import { ProviderOption, RegistryFilters, RegistryMeta, UserRegistryMeta } from "../types/registry";
import { BackendService } from "./api";

export class TauriService implements BackendService {
  constructor() {
    invoke("register_user_agent", { ua: navigator.userAgent }).catch((err) => {
      console.error("Failed to register User-Agent:", err);
    });
  }

  async getAllRegistries(payload: CursorPayload<RegistryFilters>): Promise<CursorResponse<RegistryMeta>> {
    const res = await invoke<any>("get_all_registries", { payload });
    return {
      data: res.data.map((r: any) => {
        if (Array.isArray(r.source_types)) {
          r.source_types = new Set(r.source_types);
        }
        return r as RegistryMeta;
      }),
      next_cursor: res.next_cursor,
    };
  }

  async getRegistryMeta(id: number): Promise<RegistryMeta> {
    const res = await invoke<any>("get_registry", { id });

    if (Array.isArray(res.source_types)) {
      res.source_types = new Set(res.source_types);
    }
    return res as RegistryMeta;
  }

  async saveRegistryMeta(id: number, meta: Partial<UserRegistryMeta>): Promise<void> {
    const payload = {
      ...meta,
      source_types: meta.source_types ? Array.from(meta.source_types) : undefined,
    };
    await invoke("save_registry_meta", { id, meta: payload });
  }

  async addRegistry(url: string, providerId: string): Promise<RegistryMeta> {
    const res = await invoke<any>("add_registry", { url, providerId });
    if (Array.isArray(res.source_types)) {
      res.source_types = new Set(res.source_types);
    }
    return res as RegistryMeta;
  }

  async deleteRegistry(id: number): Promise<void> {
    await invoke("delete_registry", { id });
  }

  async getProvidersForUrl(url: string): Promise<ProviderOption[]> {
    const res = await invoke<any>("get_providers_for_url", { url });
    return res.map((r: any) => r as ProviderOption);
  }

  async getAvailablePlaces(): Promise<string[]> {
    // TODO: Implement backend extraction of unique places
    console.warn("getAvailablePlaces not implemented");
    return [];
  }

  async getAvailableCollections(): Promise<string[]> {
    // TODO: Implement backend extraction of unique collections
    console.warn("getAvailableCollections not implemented");
    return [];
  }

  async getImageMeta(registryId: number, imageId: number): Promise<ImageMeta> {
    const res = await invoke<any>("get_image_meta", { registryId, imageId });
    if (typeof res.act_types === "object" && res.act_types !== null) res.act_types = new Map(Object.entries(res.act_types));
    else res.act_types = new Map();
    return res as ImageMeta;
  }

  async saveImageMeta(registryId: number, imageId: number, meta: Partial<UserImageMeta>): Promise<void> {
    await invoke("save_image_meta", { registryId, imageId, meta });
  }

  getTileUrl(registryId: number, imageId: number, level: number, x: number, y: number): string | null {
    return `tiles://localhost/${registryId}/${imageId}/${level}/${x}/${y}`;
  }

  getImageUrl(registryId: number, imageId: number): string | null {
    return `tiles://localhost/${registryId}/${imageId}`;
  }

  async downloadImage(registryId: number, imageId: number): Promise<void> {
    await invoke("download_image", { registryId, imageId });
  }

  async onDownloadProgress(registryId: number, imageId: number, cb: (current: number, total: number) => void): Promise<() => void> {
    return await listen<{ registry_id: number; image_id: number; current: number; total: number }>("download-progress", (event) => {
      if (event.payload.registry_id === registryId && event.payload.image_id === imageId) {
        cb(event.payload.current, event.payload.total);
      }
    });
  }

  async getEventRows(registryId: number): Promise<EventRow[]> {
    return await invoke<EventRow[]>("get_event_rows", { registryId });
  }

  async getEventDetail(eventId: number): Promise<EventDetail | null> {
    return await invoke<EventDetail | null>("get_event_detail", { eventId });
  }

  async saveAct(event: EventDetail): Promise<void> {
    await invoke("save_act", { event });
  }
}
