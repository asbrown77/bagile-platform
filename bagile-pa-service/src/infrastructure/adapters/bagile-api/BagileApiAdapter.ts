import type {
  IBagileApiPort,
  PendingTransfer,
  CourseAtRisk,
} from "../../../domain/ports/IBagileApiPort.js";
import { fetchJson } from "../../http/fetchJson.js";

interface MonitoringItem {
  id: number;
  courseCode: string;
  title: string;
  startDate: string;
  currentEnrolmentCount: number;
  minimumRequired: number;
  daysUntilDecision: number;
  recommendedAction: string;
  monitoringStatus: string;
}

export class BagileApiAdapter implements IBagileApiPort {
  private readonly baseUrl: string;
  private readonly apiKey: string;

  constructor(baseUrl: string, apiKey: string) {
    this.baseUrl = baseUrl;
    this.apiKey = apiKey;
  }

  private get headers(): Record<string, string> {
    return {
      "X-Api-Key": this.apiKey,
      Accept: "application/json",
    };
  }

  async getPendingTransfers(): Promise<PendingTransfer[]> {
    return fetchJson<PendingTransfer[]>(
      `${this.baseUrl}/api/transfers/pending`,
      { headers: this.headers }
    );
  }

  async getCoursesAtRisk(daysAhead: number): Promise<CourseAtRisk[]> {
    const items = await fetchJson<MonitoringItem[]>(
      `${this.baseUrl}/api/course-schedules/monitoring?daysAhead=${daysAhead}`,
      { headers: this.headers }
    );

    return items.map((item) => ({
      courseId: item.id,
      courseCode: item.courseCode,
      title: item.title,
      startDate: item.startDate,
      currentEnrolmentCount: item.currentEnrolmentCount,
      minimumRequired: item.minimumRequired,
      daysUntilDecision: item.daysUntilDecision,
      recommendedAction: item.recommendedAction,
      monitoringStatus: item.monitoringStatus,
    }));
  }
}
