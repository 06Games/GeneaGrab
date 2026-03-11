export const EVENT_TYPE_OPTIONS = [
  "Birth", "Marriage", "Death", "Burial", "Census", "Notarial", "Other", // TODO: Setup translations and use API-driven values for suggestions
] as const;
export type EventType = typeof EVENT_TYPE_OPTIONS[number];

export interface RegistryMeta {
  registry_id: string;
  archive_reference: string;
  source_types: Set<EventType | string>;
  town: string;
  repository_url: string;
  total_images: number;
}

/** Editable image metadata */
export interface UserImageMeta {
  name?: string;
  date_range?: string;
  notes?: string;
}

/** Read-only image metadata */
export interface ImageMeta extends UserImageMeta {
  image_number: number;
  act_types: Map<EventType, number>;
}

export type EventRow = {
  event_id: number;
  date: string;
  event_type: EventType;
  title: string;
}

export type PersonEntry = {
  person_id: string;
  role: string;
  first_name: string;
  last_name: string;
  sex: string;
  title: string;
  age: string;
  is_deceased: boolean;
  occupation: string;
  origin_place: string;
  residence_place: string;
  sequence_number: string;
  notes: string;
  relationship_type: string;
  relationship_to: string;
}

export type EventDetail = {
  event_id: number;
  date: string;
  date_normalized: string;
  event_type: EventType;
  title: string;
  act_number: string;
  page: string;
  image_number: string;
  town: string;
  parish: string;
  hamlet: string;
  transcription_text: string;
  notes: string;
  people: PersonEntry[];
}

export interface PluginOption {
  id: string;
  name: string;
}

// TODO: Use previously written values for suggestions

export const ROLE_SUGGESTIONS = [
  "Sujet principal", "Époux", "Épouse", "Père", "Mère", "Témoin", "Déclarant", "Parrain", "Marraine"
];

export const RELATION_SUGGESTIONS = [
  "Époux de", "Épouse de", "Fils de", "Fille de", "Frère de", "Sœur de", "Veuve de", "Veuf de"
];

export const PROFESSION_OPTIONS = [
  "Laboureur", "Tisserand", "Notaire", "Charpentier", "Cordonnier", "Cultivateur", "Ménagère", "Journalier", "Propriétaire"
];
