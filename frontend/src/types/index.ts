import { ActType } from "./registry";

export type EventRow = {
  event_id: number;
  date: string;
  event_type: ActType;
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
  event_type: ActType;
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

