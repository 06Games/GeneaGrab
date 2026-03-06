// ─── Registry & page metadata ─────────────────────────────────────────────────

export interface RegistryMeta {
  archive: string;
  fond: string;
  cote: string;
  commune: string;
  period: string;
  type: string;
  source: string;
}

export interface PageMeta {
  folio: string;
  dateRange: string;
  actCount: number;
  indexedCount: number;
  actTypes: ActType[];
}

// ─── Acts ─────────────────────────────────────────────────────────────────────

export type ActType =
  | "Naissance"
  | "Mariage"
  | "Décès"
  | "Reconnaissance"
  | "Autre";

export interface ActRow {
  id: number;
  date: string;
  type: ActType;
  subject: string;
}

// ─── People inside an act ─────────────────────────────────────────────────────

/**
 * PersonEntry — one person within an act.
 * `relation` is free-form text with autocomplete suggestions; it is NOT
 * an enum so users can enter anything (e.g. "Beau-père", "Curé").
 */
export interface PersonEntry {
  id: string;
  relation: string;
  nom: string;
  prenoms: string;
  ageOrBorn: string;
  profession: string;
  domicile: string;
}

export interface ActDetail {
  id: number;
  date: string;
  type: ActType;
  folioRef: string;
  actNumber: string;
  people: PersonEntry[];
  remarks: string;
}

// ─── Common relation suggestions ─────────────────────────────────────────────

export const RELATION_SUGGESTIONS: string[] = [
  "Déclarant", "Déclarante", "Père", "Mère", "Époux", "Épouse",
  "Enfant", "Parrain", "Marraine", "Témoin", "Grand-père", "Grand-mère",
  "Oncle", "Tante", "Frère", "Sœur", "Tuteur",
  "Officier d'état civil", "Curé", "Médecin", "Sage-femme",
];
