import apiClient from '../../../api/apiClient';

export const DashboardActivityStatus = {
  Queued: 0,
  Processing: 1,
  Completed: 2,
  Failed: 3,
  Cancelled: 4,
} as const;
export type DashboardActivityStatus = (typeof DashboardActivityStatus)[keyof typeof DashboardActivityStatus];

export type ActivityEntityType = 'Dataset' | 'Model' | 'Forecast';

export interface RecentActivityItem {
  entityType: ActivityEntityType;
  entityId: number;
  projectId: number;
  datasetId: number | null;
  name: string | null;
  status: DashboardActivityStatus;
  progressPercentage: number;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export const getRecentActivity = async (): Promise<RecentActivityItem[]> => {
  const response = await apiClient.get<RecentActivityItem[]>('/dashboard/recent-activity');
  return response.data;
};

export const dismissActivity = async (
  entityType: ActivityEntityType,
  entityId: number
): Promise<void> => {
  await apiClient.post('/dashboard/recent-activity/dismiss', { entityType, entityId });
};
