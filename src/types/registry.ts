export type EventType = 
  | "Naissance" 
  | "Mariage" 
  | "Décès" 
  | "Sépulture" 
  | "Testament" 
  | "Recensement" 
  | "Autre";

export interface RegistryMeta {
  source_id: string;
  archive_reference: string;
  source_types: Set<EventType | string>;
  town: string;
  repository_url: string;
}

export interface ImageMeta {
  folio: string;
  dateRange: string;
  actTypes: Map<EventType, number>;
}

export interface EventRow {
  event_id: number;
  date: string;
  event_type: EventType;
  title: string;
}

export interface PersonEntry {
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

export interface EventDetail {
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

// Shared UI Constants
export const EVENT_TYPE_OPTIONS: EventType[] = [
  "Naissance", "Mariage", "Décès", "Sépulture", "Testament", "Recensement", "Autre",
];

export const ROLE_SUGGESTIONS = [
  "Sujet principal", "Époux", "Épouse", "Père", "Mère", "Témoin", "Déclarant", "Parrain", "Marraine"
];

export const RELATION_SUGGESTIONS = [
  "Époux de", "Épouse de", "Fils de", "Fille de", "Frère de", "Sœur de", "Veuve de", "Veuf de"
];

export const PROFESSION_OPTIONS = [
  "Laboureur", "Tisserand", "Notaire", "Charpentier", "Cordonnier", "Cultivateur", "Ménagère", "Journalier", "Propriétaire"
];
