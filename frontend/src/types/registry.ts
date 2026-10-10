import { UserImageMeta } from "./image";

export const ACT_TYPE_OPTIONS = ["vital", "union", "mortality", "census", "legal", "land", "media", "military", "other", "unknown"] as const;
export type ActTypeCategory = (typeof ACT_TYPE_OPTIONS)[number];

export const ACT_CATEGORY_META: Record<ActTypeCategory, { icon: string; style: string }> = {
  vital: { icon: "lucide:baby", style: "text-event-vital bg-event-vital-bg border-event-vital-border" },
  union: { icon: "lucide:heart", style: "text-event-union bg-event-union-bg border-event-union-border" },
  mortality: { icon: "lucide:cross", style: "text-event-mortality bg-event-mortality-bg border-event-mortality-border" },
  census: { icon: "lucide:users", style: "text-event-census bg-event-census-bg border-event-census-border" },
  legal: { icon: "lucide:scale", style: "text-event-legal bg-event-legal-bg border-event-legal-border" },
  land: { icon: "lucide:map", style: "text-event-land bg-event-land-bg border-event-land-border" },
  military: { icon: "lucide:shield", style: "text-event-military bg-event-military-bg border-event-military-border" },
  media: { icon: "lucide:image", style: "text-event-media bg-event-media-bg border-event-media-border" },
  other: { icon: "lucide:file-text", style: "text-event-other bg-event-other-bg border-event-other-border" },
  unknown: { icon: "lucide:help-circle", style: "text-event-other bg-event-other-bg border-event-other-border" },
};

export type ActType = {
  category: ActTypeCategory;
  label: string | null;
};

export interface RegistryFilters {
  search_term?: string | null;
  source_type?: string | null;
  place?: string | null;
  collection?: string | null;
  date_from?: string | null;
  date_to?: string | null;
  location?: string[] | null;
  is_unknown_location?: boolean | null;
}

export interface RegistryMeta {
  id: number;
  source_id: string;
  registry_id: string;
  archive_reference: string;
  source_types: Set<ActType>;
  places: string[][];
  collection: string[];
  ark_url?: string;

  title?: string;
  subtitle?: string;
  author?: string;
  date_from?: string;
  date_to?: string;
  notes?: string;

  total_images: number;
  images: UserImageMeta[] | undefined;
  acts_count: number;
}

export interface UserRegistryMeta {
  archive_reference?: string;
  title?: string;
  subtitle?: string;
  author?: string;
  date_from?: string;
  date_to?: string;
  places?: string[][];
  collection?: string[];
  source_types?: Set<ActType> | ActType[];
  ark_url?: string;
  notes?: string;
}

export interface ProviderOption {
  id: string;
  name: string;
  registry_id?: string;
  image_number?: number;
}

export interface AvailableOption {
  key: string;
  label: string;
  count: number;
  parts?: string[];
}
