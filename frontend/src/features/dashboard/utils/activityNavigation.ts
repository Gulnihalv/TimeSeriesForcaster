import { DashboardActivityStatus, type RecentActivityItem } from '../api/dashboardApi';

export const isTerminalStatus = (status: DashboardActivityStatus) =>
  status === DashboardActivityStatus.Completed ||
  status === DashboardActivityStatus.Failed ||
  status === DashboardActivityStatus.Cancelled;

export const buildActivityPath = (item: Pick<RecentActivityItem, 'entityType' | 'entityId' | 'datasetId'>): string =>
  item.entityType === 'Dataset'
    ? `/datasets/${item.entityId}`
    : `/datasets/${item.datasetId}?modelId=${item.entityId}`; // Model ve Forecast aynı Model satırına işaret eder
