import type { FC } from 'react';
import { RadialBarChart, RadialBar, PolarAngleAxis } from 'recharts';
import { LuX, LuFolderKanban, LuCpu, LuTrendingUp } from 'react-icons/lu';
import Card from '../../../components/Card/Card';
import { DashboardActivityStatus, type RecentActivityItem } from '../api/dashboardApi';
import { isTerminalStatus } from '../utils/activityNavigation';
import styles from './ActivityCard.module.css';

const STATUS_COLOR_VAR: Record<DashboardActivityStatus, string> = {
  [DashboardActivityStatus.Queued]: 'var(--color-status-queued-text)',
  [DashboardActivityStatus.Processing]: 'var(--color-status-training-text)',
  [DashboardActivityStatus.Completed]: 'var(--color-status-completed-text)',
  [DashboardActivityStatus.Failed]: 'var(--color-status-failed-text)',
  [DashboardActivityStatus.Cancelled]: 'var(--color-status-cancelled-text)',
};

const STATUS_TRACK_VAR: Record<DashboardActivityStatus, string> = {
  [DashboardActivityStatus.Queued]: 'var(--color-status-queued-bg)',
  [DashboardActivityStatus.Processing]: 'var(--color-status-training-bg)',
  [DashboardActivityStatus.Completed]: 'var(--color-status-completed-bg)',
  [DashboardActivityStatus.Failed]: 'var(--color-status-failed-bg)',
  [DashboardActivityStatus.Cancelled]: 'var(--color-status-cancelled-bg)',
};

const STATUS_LABELS: Record<'Dataset' | 'Model' | 'Forecast', Record<DashboardActivityStatus, string>> = {
  Dataset: {
    [DashboardActivityStatus.Queued]: 'Kuyrukta',
    [DashboardActivityStatus.Processing]: 'Yükleniyor',
    [DashboardActivityStatus.Completed]: 'Tamamlandı',
    [DashboardActivityStatus.Failed]: 'Hata',
    [DashboardActivityStatus.Cancelled]: 'İptal Edildi',
  },
  Model: {
    [DashboardActivityStatus.Queued]: 'Kuyrukta',
    [DashboardActivityStatus.Processing]: 'Eğitiliyor',
    [DashboardActivityStatus.Completed]: 'Tamamlandı',
    [DashboardActivityStatus.Failed]: 'Hata',
    [DashboardActivityStatus.Cancelled]: 'İptal Edildi',
  },
  Forecast: {
    [DashboardActivityStatus.Queued]: 'Kuyrukta',
    [DashboardActivityStatus.Processing]: 'Tahmin Üretiliyor',
    [DashboardActivityStatus.Completed]: 'Tamamlandı',
    [DashboardActivityStatus.Failed]: 'Hata',
    [DashboardActivityStatus.Cancelled]: 'İptal Edildi',
  },
};

const ENTITY_TYPE_LABELS: Record<'Dataset' | 'Model' | 'Forecast', string> = {
  Dataset: 'Dataset',
  Model: 'Model',
  Forecast: 'Tahmin',
};

const ENTITY_TYPE_ICONS: Record<'Dataset' | 'Model' | 'Forecast', FC<{ size?: number }>> = {
  Dataset: LuFolderKanban,
  Model: LuCpu,
  Forecast: LuTrendingUp,
};

const ENTITY_TYPE_ICON_COLOR_VAR: Record<'Dataset' | 'Model' | 'Forecast', string> = {
  Dataset: 'var(--color-tone-blue-text)',
  Model: 'var(--color-tone-violet-text)',
  Forecast: 'var(--color-tone-amber-text)',
};

const ENTITY_TYPE_ICON_BG_VAR: Record<'Dataset' | 'Model' | 'Forecast', string> = {
  Dataset: 'var(--color-tone-blue-bg)',
  Model: 'var(--color-tone-violet-bg)',
  Forecast: 'var(--color-tone-amber-bg)',
};

const ACTION_LABELS: Record<DashboardActivityStatus, string | null> = {
  [DashboardActivityStatus.Queued]: null,
  [DashboardActivityStatus.Processing]: null,
  [DashboardActivityStatus.Completed]: 'Görüntüle',
  [DashboardActivityStatus.Failed]: 'Tekrar dene',
  [DashboardActivityStatus.Cancelled]: 'İptal Edildi',
};

const formatRelativeTime = (iso: string) => {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return 'az önce';
  if (minutes < 60) return `${minutes} dk önce`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} sa önce`;
  return new Date(iso).toLocaleDateString('tr-TR');
};

interface ActivityCardProps {
  item: RecentActivityItem;
  onClick: (item: RecentActivityItem) => void;
  onDismiss: (item: RecentActivityItem) => void;
}

const ActivityCard: FC<ActivityCardProps> = ({ item, onClick, onDismiss }) => {
  const color = STATUS_COLOR_VAR[item.status];
  const track = STATUS_TRACK_VAR[item.status];
  const label = STATUS_LABELS[item.entityType][item.status];
  const actionLabel = ACTION_LABELS[item.status];
  const EntityIcon = ENTITY_TYPE_ICONS[item.entityType];
  const chartData = [{ value: item.progressPercentage, fill: color }];

  return (
    <Card interactive className={styles.card} onClick={() => onClick(item)}>
      {isTerminalStatus(item.status) && (
        <button
          className={styles.dismissButton}
          aria-label="Kaldır"
          title="Kaldır"
          onClick={(e) => {
            e.stopPropagation();
            onDismiss(item);
          }}
        >
          <LuX size={14} />
        </button>
      )}

      <div className={styles.header}>
        <div
          className={styles.entityIconBadge}
          style={{ color: ENTITY_TYPE_ICON_COLOR_VAR[item.entityType], backgroundColor: ENTITY_TYPE_ICON_BG_VAR[item.entityType] }}
        >
          <EntityIcon size={18} />
        </div>
        <div className={styles.chartWrap}>
          <RadialBarChart
            width={72}
            height={72}
            innerRadius="70%"
            outerRadius="100%"
            data={chartData}
            startAngle={90}
            endAngle={-270}
          >
            <PolarAngleAxis type="number" domain={[0, 100]} angleAxisId={0} tick={false} />
            <RadialBar dataKey="value" background={{ fill: track }} cornerRadius={8} isAnimationActive={false} />
          </RadialBarChart>
          <span className={styles.percentageLabel} style={{ color }}>
            {item.progressPercentage}%
          </span>
        </div>
      </div>

      <div className={styles.info}>
        <span className={styles.entityType}>{ENTITY_TYPE_LABELS[item.entityType]}</span>
        <h4 className={styles.name}>{item.name || '—'}</h4>
        <span className={styles.statusLabel} style={{ color }}>
          {label}
        </span>
      </div>

      <div className={styles.footer}>
        <span className={styles.timeLabel}>{formatRelativeTime(item.createdAt)}</span>
        {actionLabel && <span className={styles.actionLabel}>{actionLabel}</span>}
      </div>
    </Card>
  );
};

export default ActivityCard;
