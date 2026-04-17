// Matches PendingTransferDto from the BAgile platform API
export interface PendingTransfer {
  studentId: number;
  studentName: string;
  studentEmail: string;
  organisation?: string;
  cancelledScheduleId: number;
  courseCode: string;
  courseTitle: string;
  originalStartDate?: string;
  cancelledDate: string;
  daysSinceCancellation: number;
}

// Matches /api/course-schedules/monitoring response
export interface CourseAtRisk {
  courseId: number;
  courseCode: string;
  title: string;
  startDate: string;
  currentEnrolmentCount: number;
  minimumRequired: number;
  daysUntilDecision: number;
  recommendedAction: string;
  monitoringStatus: string;
}

export interface IBagileApiPort {
  getPendingTransfers(): Promise<PendingTransfer[]>;
  getCoursesAtRisk(daysAhead: number): Promise<CourseAtRisk[]>;
}
