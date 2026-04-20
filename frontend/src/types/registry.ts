export const ACT_TYPE_OPTIONS = ["vital", "union", "mortality", "census", "legal", "land", "media", "military", "other", "unknown"] as const;
export type ActTypeCategory = (typeof ACT_TYPE_OPTIONS)[number];

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
}

export interface RegistryMeta {
  id: number;
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
  acts_count: number;
}

export interface PluginOption {
  id: string;
  name: string;
}
