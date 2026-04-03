import { ActType } from "./registry";

/** Editable image metadata */
export interface UserImageMeta {
  name?: string;
  date_range?: string;
  notes?: string;
}

/** Read-only image metadata */
export interface ImageMeta extends UserImageMeta {
  image_number: number;
  act_types: Map<ActType, number>;
  width: number,
  height: number,
  tile_size: number
}
