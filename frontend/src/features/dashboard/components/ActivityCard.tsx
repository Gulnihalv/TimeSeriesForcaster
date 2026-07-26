import type { FC } from 'react';
import { RadialBarChart, RadialBar, PolarAngleAxis } from 'recharts';
import { LuX } from 'react-icons/lu';
import Card from '../../../components/Card/Card';
import { DashboardActivityStatus, type RecentActivityItem } from '../api/dashboardApi';
import { isTerminalStatus } from '../utils/activityNavigation';
import styles from './ActivityCard.module.css';

const STATUS_COLOR_VAR: Record<DashboardActivityStatus, string> = {
  [DashboardActivityStatus.Queued]: 'var(--color-status-queued-text)',
  [DashboardActivityStatus.Processing]: 'var(--color-status-training-text)',
  [DashboardActivityStatus.Completed]: 'var(--color-status-completed-text)',
  [DashboardActivityStatus.Failed]: 'var(--color-status-failed-text)',
};

const STATUS_TRACK_VAR: Record<DashboardActivityStatus, string> = {
  [DashboardActivityStatus.Queued]: 'var(--color-status-queued-bg)',
  [DashboardActivityStatus.Processing]: 'var(--color-status-training-bg)',
  [DashboardActivityStatus.Completed]: 'var(--color-status-completed-bg)',
  [DashboardActivityStatus.Failed]: 'var(--color-status-failed-bg)',
};

const STATUS_LABELS: Record<'Dataset' | 'Model' | 'Forecast', Record<DashboardActivityStatus, string>> = {
  Dataset: {
    [DashboardActivityStatus.Queued]: 'Kuyrukta',
    [DashboardActivityStatus.Processing]: 'Yükleniyor',
    [DashboardActivityStatus.Completed]: 'Tamamlandı',
    [DashboardActivityStatus.Failed]: 'Hata',
  },
  Model: {
    [DashboardActivityStatus.Queued]: 'Kuyrukta',
    [DashboardActivityStatus.Processing]: 'Eğitiliyor',
    [DashboardActivityStatus.Completed]: 'Tamamlandı',
    [DashboardActivityStatus.Failed]: 'Hata',
  },
  Forecast: {
    [DashboardActivityStatus.Queued]: 'Kuyrukta',
    [DashboardActivityStatus.Processing]: 'Tahmin Üretiliyor',
    [DashboardActivityStatus.Completed]: 'Tamamlandı',
    [DashboardActivityStatus.Failed]: 'Hata',
  },
};

const ENTITY_TYPE_LABELS: Record<'Dataset' | 'Model' | 'Forecast', string> = {
  Dataset: 'Dataset',
  Model: 'Model',
  Forecast: 'Tahmin',
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

      <div className={styles.info}>
        <span className={styles.entityType}>{ENTITY_TYPE_LABELS[item.entityType]}</span>
        <h4 className={styles.name}>{item.name || '—'}</h4>
        <span className={styles.statusLabel} style={{ color }}>
          {label}
        </span>
      </div>

      <div className={styles.chartWrap}>
        <RadialBarChart
          width={64}
          height={64}
          innerRadius="70%"
          outerRadius="100%"
          data={chartData}
          startAngle={90}
          endAngle={-270}
        >
          <PolarAngleAxis type="number" domain={[0, 100]} angleAxisId={0} tick={false} />
          <RadialBar dataKey="value" background={{ fill: track }} cornerRadius={8} isAnimationActive={false} />
        </RadialBarChart>
        <span className={styles.percentageLabel}>{item.progressPercentage}%</span>
      </div>
    </Card>
  );
};

export default ActivityCard;
