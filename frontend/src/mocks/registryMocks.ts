import { EventDetail, EventRow } from "../types";
import { ImageMeta } from "../types/image";
import { ActType, RegistryMeta } from "../types/registry";

export const MOCK_REGISTRY_DATA: RegistryMeta = {
  id: 123,
  archive_reference: "5 Mi 1/342",
  source_types: new Set([
    { category: "vital", label: "Birth" },
    { category: "union", label: "Marriage" },
    { category: "mortality", label: "Death" },
  ]),
  places: [["Brignoles"], ["Vins-sur-Caramy"]],
  collection: ["État civil"],
  ark_url: "https://archives.var.fr/ark:/...",
  title: "Registres d'état civil de Brignoles",
  date_from: "1793",
  date_to: "1794",
  total_images: 348,
  acts_count: 42,
};

export const MOCK_IMAGE_META: ImageMeta = {
  image_number: 12,
  width: 512,
  height: 512,
  tile_size: 512,
  date_range: "1793-11-01 to 1793-11-30",
  notes: "Image in good condition, but handwriting is difficult to read in some areas.",
  ark_url: "",
  act_types: new Map<ActType, number>([
    [{ category: "vital", label: "Birth" }, 2],
    [{ category: "union", label: "Marriage" }, 1],
    [{ category: "mortality", label: "Death" }, 1],
  ]),
};

export const MOCK_EVENT_ROWS: EventRow[] = [
  { event_id: 1, date: "03 Frim. II", event_type: { category: "vital", label: "Birth" }, title: "MARTIN, Jean-Baptiste" },
  { event_id: 2, date: "03 Frim. II", event_type: { category: "vital", label: "Birth" }, title: "DUPONT, Marie" },
  { event_id: 3, date: "05 Frim. II", event_type: { category: "union", label: "Marriage" }, title: "ARNAUD, Pierre ∞ BLANC" },
  { event_id: 4, date: "05 Frim. II", event_type: { category: "mortality", label: "Death" }, title: "BOYER, Antoinette" },
  { event_id: 5, date: "07 Frim. II", event_type: { category: "vital", label: "Birth" }, title: "ISNARD, Louis" },
  { event_id: 6, date: "12 Frim. II", event_type: { category: "vital", label: "Birth" }, title: "FABRE, Thérèse" },
  { event_id: 7, date: "14 Frim. II", event_type: { category: "union", label: "Marriage" }, title: "ROUX, Antoine ∞ AUBERT" },
  { event_id: 8, date: "16 Frim. II", event_type: { category: "mortality", label: "Death" }, title: "PASCAL, Jean" },
];

export const MOCK_SELECTED_EVENT: EventDetail = {
  event_id: 3,
  date: "05 Frimaire An II",
  date_normalized: "1793-11-25",
  event_type: { category: "union", label: "Marriage" },
  title: "Marriage ARNAUD, Pierre ∞ BLANC",
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
      relationship_to: "Marie BLANC",
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
      relationship_to: "Pierre ARNAUD",
    },
  ],
};
