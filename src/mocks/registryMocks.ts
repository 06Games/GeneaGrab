import type { RegistryMeta, ImageMeta, EventRow, EventDetail, EventType } from "../types/registry";

export const MOCK_REGISTRY_DATA: RegistryMeta = {
  source_id: "src-123",
  archive_reference: "5 Mi 1/342",
  source_types: new Set(["Naissance", "Mariage", "Décès"]),
  town: "Brignoles",
  repository_url: "https://archives.var.fr",
  total_images: 348,
};

export const MOCK_IMAGE_META: ImageMeta = {
  folio: "12r",
  dateRange: "3 Frimaire An II",
  actTypes: new Map<EventType, number>([
    ["Naissance", 2],
    ["Mariage", 1],
    ["Décès", 1],
  ]),
};

export const MOCK_EVENT_ROWS: EventRow[] = [
  { event_id: 1, date: "03 Frim. II", event_type: "Naissance", title: "MARTIN, Jean-Baptiste" },
  { event_id: 2, date: "03 Frim. II", event_type: "Naissance", title: "DUPONT, Marie" },
  { event_id: 3, date: "05 Frim. II", event_type: "Mariage",   title: "ARNAUD, Pierre ∞ BLANC" },
  { event_id: 4, date: "05 Frim. II", event_type: "Décès",     title: "BOYER, Antoinette" },
  { event_id: 5, date: "07 Frim. II", event_type: "Naissance", title: "ISNARD, Louis" },
  { event_id: 6, date: "12 Frim. II", event_type: "Naissance", title: "FABRE, Thérèse" },
  { event_id: 7, date: "14 Frim. II", event_type: "Mariage",   title: "ROUX, Antoine ∞ AUBERT" },
  { event_id: 8, date: "16 Frim. II", event_type: "Décès",     title: "PASCAL, Jean" },
];

export const MOCK_SELECTED_EVENT: EventDetail = {
  event_id: 3, 
  date: "05 Frimaire An II", 
  date_normalized: "1793-11-25",
  event_type: "Mariage", 
  title: "Mariage ARNAUD, Pierre ∞ BLANC",
  act_number: "47", 
  page: "12r", 
  image_number: "12",
  town: "Brignoles", 
  parish: "", 
  hamlet: "",
  transcription_text: "", 
  notes: "",
  people: [
    {
      person_id: "p1", 
      role: "Sujet principal",
      first_name: "Pierre", 
      last_name: "ARNAUD", 
      sex: "M", 
      title: "", 
      age: "27 ans",
      is_deceased: false, 
      occupation: "Laboureur", 
      origin_place: "Brignoles", 
      residence_place: "Brignoles",
      sequence_number: "", 
      notes: "", 
      relationship_type: "Époux de", 
      relationship_to: "Marie BLANC"
    },
    {
      person_id: "p2", 
      role: "Sujet principal",
      first_name: "Marie", 
      last_name: "BLANC", 
      sex: "F", 
      title: "", 
      age: "22 ans",
      is_deceased: false, 
      occupation: "", 
      origin_place: "Brignoles", 
      residence_place: "Brignoles",
      sequence_number: "", 
      notes: "", 
      relationship_type: "Épouse de", 
      relationship_to: "Pierre ARNAUD"
    },
  ]
};
