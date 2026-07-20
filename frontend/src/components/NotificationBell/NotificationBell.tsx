import { useState, type FC } from 'react';
import { LuBell } from 'react-icons/lu';
import { useApiData } from '../../hooks/useApiData';
import {
  getNotifications,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
  NotificationType,
  type Notification,
} from '../../features/notifications/api/notificationApi';
import styles from './NotificationBell.module.css';

const TYPE_LABELS: Record<NotificationType, string> = {
  [NotificationType.ModelTrainingCompleted]: 'Model eğitimi tamamlandı',
  [NotificationType.ModelTrainingFailed]: 'Model eğitimi başarısız',
  [NotificationType.DatasetProcessingCompleted]: 'Dataset işlendi',
  [NotificationType.DatasetProcessingFailed]: 'Dataset işlenemedi',
};

const formatRelativeTime = (iso: string) => {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return 'az önce';
  if (minutes < 60) return `${minutes} dk önce`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} sa önce`;
  return new Date(iso).toLocaleDateString();
};

const NotificationBell: FC = () => {
  const [isOpen, setIsOpen] = useState(false);

  // Zil rozeti, panel kapalıyken de periyodik olarak güncellensin diye ayrı bir sorgu.
  const { data: unreadCount, refetch: refetchCount } = useApiData<number>(
    getUnreadCount,
    [],
    { fallbackErrorMessage: 'Bildirim sayısı yüklenemedi.', pollIntervalMs: 20000, shouldPoll: () => true }
  );

  // Liste sadece panel açıldığında çekiliyor - gereksiz sürekli sorgu atılmasın diye.
  const { data: notifications, refetch: refetchList } = useApiData<Notification[]>(
    () => (isOpen ? getNotifications() : Promise.resolve([])),
    [isOpen],
    { fallbackErrorMessage: 'Bildirimler yüklenemedi.' }
  );

  const handleItemClick = async (notification: Notification) => {
    if (!notification.isRead) {
      await markAsRead(notification.id);
      refetchCount();
      refetchList();
    }
  };

  const handleMarkAllAsRead = async () => {
    await markAllAsRead();
    refetchCount();
    refetchList();
  };

  const hasUnread = unreadCount !== null && unreadCount > 0;

  return (
    <div className={styles.wrapper}>
      <button className={styles.iconButton} aria-label="Bildirimler" onClick={() => setIsOpen((prev) => !prev)}>
        <LuBell size={19} />
        {hasUnread && <span className={styles.badge}>{unreadCount! > 9 ? '9+' : unreadCount}</span>}
      </button>

      {isOpen && (
        <>
          <div className={styles.backdrop} onClick={() => setIsOpen(false)} />
          <div className={styles.panel}>
            <div className={styles.panelHeader}>
              <span>Bildirimler</span>
              {hasUnread && (
                <button className={styles.markAllButton} onClick={handleMarkAllAsRead}>
                  Tümünü okundu işaretle
                </button>
              )}
            </div>

            {(!notifications || notifications.length === 0) ? (
              <p className={styles.empty}>Henüz bildirim yok.</p>
            ) : (
              <ul className={styles.list}>
                {notifications.map((n) => (
                  <li
                    key={n.id}
                    className={`${styles.item} ${!n.isRead ? styles.unread : ''}`}
                    onClick={() => handleItemClick(n)}
                  >
                    <div className={styles.itemHeader}>
                      <span className={styles.itemType}>{TYPE_LABELS[n.type]}</span>
                      <span className={styles.itemTime}>{formatRelativeTime(n.createdAt)}</span>
                    </div>
                    <p className={styles.itemMessage}>{n.message}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default NotificationBell;