export interface CalendarEvent {
  id: string;
  title: string;
  start: string;
  end: string;
  location?: string;
}

export interface ICalendarPort {
  getTodayEvents(): Promise<CalendarEvent[]>;
}
