import type { CalendarEvent, ICalendarPort } from "../../../domain/ports/ICalendarPort.js";

export class CalendarStubAdapter implements ICalendarPort {
  async getTodayEvents(): Promise<CalendarEvent[]> {
    return [];
  }
}
