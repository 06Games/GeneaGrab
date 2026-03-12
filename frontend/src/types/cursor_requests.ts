export interface CursorPayload<T> {
  limit: number;
  cursor?: number | null;
  filters?: T | null;
}

export interface CursorResponse<T> {
  data: T[];
  next_cursor: number | null;
}
