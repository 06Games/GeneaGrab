// ─── Registry & image metadata (Sources) ──────────────────────────────────────

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

// ─── Events (Acts) ────────────────────────────────────────────────────────────

export type EventType =
  | "Naissance"
  | "Mariage"
  | "Décès"
  | "Sépulture"
  | "Testament"
  | "Recensement"
  | "Autre";

export interface EventRow {
  event_id: number;
  date: string;
  event_type: EventType | string;
  title: string;
}

// ─── People inside an act ─────────────────────────────────────────────────────

/**
 * PersonEntry — one person within an act.
 * Map à la fois la table `persons`, `event_participants` et `person_relationships`
 * pour une saisie fluide "à plat" côté UI.
 */
export interface PersonEntry {
  person_id: string;
  
  // -- event_participants --
  role: string; // ex: Déclarant, Testateur, Témoin
  
  // -- persons --
  first_name: string;
  last_name: string;
  sex: string; // 'M', 'F', 'Inconnu'
  title: string; // ex: nob., Me, Révérend
  age: string;
  is_deceased: boolean;
  occupation: string;
  origin_place: string;
  residence_place: string;
  sequence_number: string;
  notes: string;

  // -- person_relationships (simplifié pour l'UI) --
  relationship_type: string; // ex: Père de, Époux de
  relationship_to: string; // ex: Jean DUPONT (ou ID)
}

export interface EventDetail {
  event_id: number;
  event_type: EventType | string;
  title: string;
  act_number: string;
  date: string; // JSON inline possible côté DB, string côté UI
  date_normalized: string;
  town: string;
  hamlet: string;
  parish: string;
  page: string;
  image_number: string;
  transcription_text: string;
  notes: string;
  
  people: PersonEntry[];
}

// ─── Suggestions communes ───────────────────────────────────────────────────

export const ROLE_SUGGESTIONS: string[] = [
  "Sujet principal", "Déclarant", "Déclarante", "Témoin", 
  "Notaire", "Curé", "Officier d'état civil", "Chef de ménage", 
  "Résident", "Mentionné", "Parrain", "Marraine"
];

export const RELATION_SUGGESTIONS: string[] = [
  "Père de", "Mère de", "Époux de", "Épouse de", 
  "Enfant de", "Veuf de", "Veuve de", "Frère de", "Sœur de"
];
