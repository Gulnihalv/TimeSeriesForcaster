import { useCallback, useState, type FC } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApiData } from '../../../hooks/useApiData';
import { useToast } from '../../../components/Toast/ToastContext';
import EmptyState from '../../../components/EmptyState/EmptyState';
import { LuActivity } from 'react-icons/lu';
import {
  getRecentActivity,
  dismissActivity,
  type RecentActivityItem,
} from '../api/dashboardApi';
import { isTerminalStatus, buildActivityPath } from '../utils/activityNavigation';
import ActivityCard from './ActivityCard';
import styles from './RecentActivityFeed.module.css';

const activityKey = (item: RecentActivityItem) => `${item.entityType}:${item.entityId}`;

const RecentActivityFeed: FC = () => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [dismissingKeys, setDismissingKeys] = useState<Set<string>>(new Set());

  const shouldPoll = useCallback(
    (items: RecentActivityItem[]) => items.some((i) => !isTerminalStatus(i.status)),
    []
  );

  const { data: activity, isLoading, refetch } = useApiData<RecentActivityItem[]>(
    getRecentActivity,
    [],
    { fallbackErrorMessage: 'Etkinlikler yüklenemedi.', shouldPoll, pollIntervalMs: 3000 }
  );

  const handleCardClick = (item: RecentActivityItem) => {
    if (isTerminalStatus(item.status)) {
      navigate(buildActivityPath(item));
    } else {
      showToast('İşlem hâlâ devam ediyor.', 'info');
    }
  };

  const handleDismiss = async (item: RecentActivityItem) => {
    const key = activityKey(item);
    setDismissingKeys((current) => new Set(current).add(key));
    try {
      await dismissActivity(item.entityType, item.entityId);
      refetch();
    } finally {
      setDismissingKeys((current) => {
        const next = new Set(current);
        next.delete(key);
        return next;
      });
    }
  };

  if (isLoading) {
    return null;
  }

  const visible = (activity ?? []).filter((item) => !dismissingKeys.has(activityKey(item)));

  if (visible.length === 0) {
    return (
      <EmptyState
        icon={<LuActivity size={22} />}
        title="Şu anda devam eden bir işlem yok"
        description="Dataset yüklediğinde veya model eğittiğinde ilerlemesini burada görebilirsin."
      />
    );
  }

  return (
    <div className={styles.scrollArea}>
      {visible.map((item) => (
        <ActivityCard
          key={activityKey(item)}
          item={item}
          onClick={handleCardClick}
          onDismiss={handleDismiss}
        />
      ))}
    </div>
  );
};

export default RecentActivityFeed;
