import React from 'react';
import { ModelStatus } from '../../features/models/api/modelApi';
import styles from './StatusBadge.module.css';

type BadgeTone = 'queued' | 'training' | 'completed' | 'failed';

const MODEL_STATUS_MAP: Record<ModelStatus, { label: string; tone: BadgeTone }> = {
  [ModelStatus.Queued]: { label: 'Kuyrukta', tone: 'queued' },
  [ModelStatus.Training]: { label: 'Eğitiliyor', tone: 'training' },
  [ModelStatus.Completed]: { label: 'Tamamlandı', tone: 'completed' },
  [ModelStatus.Failed]: { label: 'Başarısız', tone: 'failed' },
};

interface StatusBadgeProps {
  status: ModelStatus;
}

/** Model eğitim durumu için rozet. */
export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const { label, tone } = MODEL_STATUS_MAP[status];
  return <span className={`${styles.badge} ${styles[tone]}`}>{label}</span>;
};

interface ProcessingBadgeProps {
  isProcessed: boolean;
  hasError?: boolean;
}

/** Dataset işlenme durumu için rozet (ModelStatus değil, Dataset.isProcessed/errorMessage). */
export const ProcessingBadge: React.FC<ProcessingBadgeProps> = ({ isProcessed, hasError }) => {
  if (hasError) return <span className={`${styles.badge} ${styles.failed}`}>Hata</span>;
  if (!isProcessed) return <span className={`${styles.badge} ${styles.queued}`}>İşleniyor</span>;
  return <span className={`${styles.badge} ${styles.completed}`}>İşlendi</span>;
};

export default StatusBadge;
