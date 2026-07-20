import apiClient from '../../../api/apiClient';

export const NotificationType = {
  ModelTrainingCompleted: 0,
  ModelTrainingFailed: 1,
  DatasetProcessingCompleted: 2,
  DatasetProcessingFailed: 3,
} as const;
export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export interface Notification {
  id: number;
  type: NotificationType;
  message: string;
  relatedEntityType: string;
  relatedEntityId: number;
  isRead: boolean;
  createdAt: string;
}

export const getNotifications = async (): Promise<Notification[]> => {
  const response = await apiClient.get<Notification[]>('/notifications');
  return response.data;
};

export const getUnreadCount = async (): Promise<number> => {
  const response = await apiClient.get<{ count: number }>('/notifications/unread-count');
  return response.data.count;
};

export const markAsRead = async (notificationId: number): Promise<void> => {
  await apiClient.post(`/notifications/${notificationId}/read`);
};

export const markAllAsRead = async (): Promise<void> => {
  await apiClient.post('/notifications/read-all');
};