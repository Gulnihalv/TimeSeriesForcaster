import { useEffect, type FC } from 'react';
import Spinner from '../../../components/Spinner/Spinner';
import { ProcessingBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getDatasetById, getDatasetStatistics, type Dataset, type DatasetStatistics } from '../api/datasetApi';
import { describeVariability } from '../utils/dataStatistics';
import {
  LuFileText,
  LuCalendarRange,
  LuTarget,
  LuSigma,
  LuChartBar,
  LuActivity,
  LuWaves,
  LuArrowDownToLine,
  LuArrowUpToLine,
} from 'react-icons/lu';
import styles from './DatasetSummary.module.css';

interface DatasetSummaryProps {
  datasetId: number;
  /** Dataset yüklendikten sonra parent'a (örn. ModelTrainingForm'un disable durumu için) haber verir */
  onLoaded?: (dataset: Dataset) => void;
}

const formatDate = (value: string | null) =>
  value ? new Date(value).toLocaleDateString() : '—';

const formatMonthYear = (value: string | null) => {
  if (!value) return '—';
  const d = new Date(value);
  return d.toLocaleDateString('tr-TR', { month: '2-digit', year: 'numeric' });
};

const formatNumber = (value: number) =>
  value.toLocaleString('tr-TR', { maximumFractionDigits: 2 });

interface DatasetStatisticsSectionProps {
  datasetId: number;
}

/**
 * Sadece dataset işlendikten sonra mount edilir; böylece isLoading ilk render'dan itibaren
 * doğru (true) başlar ve "hesaplanamadı" gibi ara durumlar görünmez.
 */
const DatasetStatisticsSection: FC<DatasetStatisticsSectionProps> = ({ datasetId }) => {
  const { data: statistics, isLoading } = useApiData<DatasetStatistics>(
    () => getDatasetStatistics(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'İstatistikler yüklenemedi.' }
  );

  return (
    <div className={styles.statisticsSection}>
      {isLoading ? (
        <Spinner label="İstatistikler hesaplanıyor..." />
      ) : statistics ? (
        <div className={styles.statGrid}>
          <div className={styles.statChip}>
            <div className={styles.statChipHeader}>
              <span className={styles.statLabel}>Ortalama</span>
              <LuSigma size={14} className={styles.statIcon} />
            </div>
            <span className={styles.statValue}>{formatNumber(statistics.mean)}</span>
          </div>
          <div className={styles.statChip}>
            <div className={styles.statChipHeader}>
              <span className={styles.statLabel}>Medyan</span>
              <LuChartBar size={14} className={styles.statIcon} />
            </div>
            <span className={styles.statValue}>{formatNumber(statistics.median)}</span>
          </div>
          <div className={styles.statChip}>
            <div className={styles.statChipHeader}>
              <span className={styles.statLabel}>Std. sapma</span>
              <LuActivity size={14} className={styles.statIcon} />
            </div>
            <span className={styles.statValue}>{formatNumber(statistics.stdDev)}</span>
          </div>
          <div className={styles.statChip}>
            <div className={styles.statChipHeader}>
              <span className={styles.statLabel}>Değişkenlik</span>
              <LuWaves size={14} className={styles.statIcon} />
            </div>
            <span className={styles.statValue}>{describeVariability(statistics.coefficientOfVariation)}</span>
          </div>
          <div className={styles.statChip}>
            <div className={styles.statChipHeader}>
              <span className={styles.statLabel}>Min</span>
              <LuArrowDownToLine size={14} className={styles.statIcon} />
            </div>
            <span className={styles.statValue}>
              {formatNumber(statistics.min)} <small className={styles.statSub}>({formatDate(statistics.minDate)})</small>
            </span>
          </div>
          <div className={styles.statChip}>
            <div className={styles.statChipHeader}>
              <span className={styles.statLabel}>Max</span>
              <LuArrowUpToLine size={14} className={styles.statIcon} />
            </div>
            <span className={styles.statValue}>
              {formatNumber(statistics.max)} <small className={styles.statSub}>({formatDate(statistics.maxDate)})</small>
            </span>
          </div>
        </div>
      ) : (
        <p className={styles.statisticsEmpty}>İstatistik hesaplanamadı.</p>
      )}
    </div>
  );
};

const DatasetSummary: FC<DatasetSummaryProps> = ({ datasetId, onLoaded }) => {
  const { data: dataset, isLoading, error } = useApiData<Dataset>(
    () => getDatasetById(datasetId),
    [datasetId],
    {
      fallbackErrorMessage: 'Dataset bilgisi yüklenemedi.',
      shouldPoll: (d) => !d.isProcessed && !d.errorMessage,
    }
  );

  useEffect(() => {
    if (dataset && onLoaded) onLoaded(dataset);
  }, [dataset, onLoaded]);

  if (isLoading && !dataset) return <p className={styles.loading}>Dataset yükleniyor...</p>;
  if (error || !dataset) return <div className={styles.error}>{error || 'Dataset bulunamadı.'}</div>;

  return (
    <>
      <div className={styles.header}>
        <div className={styles.titleGroup}>
          <span className={styles.datasetName}>{dataset.name}</span>
          <span className={styles.fileName}>{dataset.originalFileName}</span>
        </div>
        <ProcessingBadge isProcessed={dataset.isProcessed} hasError={!!dataset.errorMessage} />
      </div>

      {dataset.errorMessage && (
        <div className={styles.error}>{dataset.errorMessage}</div>
      )}

      <div className={styles.infoList}>
        <div className={styles.infoRow}>
          <span className={styles.infoIcon}><LuFileText size={16} /></span>
          <div className={styles.infoTextStack}>
            <span className={styles.infoLabel}>Kayıt Sayısı</span>
            <span className={styles.infoValue}>{dataset.recordCount ?? '—'}</span>
          </div>
        </div>
        <div className={styles.infoRow}>
          <span className={styles.infoIcon}><LuCalendarRange size={16} /></span>
          <div className={styles.infoTextStack}>
            <span className={styles.infoLabel}>Başlangıç - Bitiş Tarihi</span>
            <span className={styles.infoValue}>
              {formatMonthYear(dataset.startDate)} - {formatMonthYear(dataset.endDate)}
            </span>
          </div>
        </div>
        <div className={styles.infoRow}>
          <span className={styles.infoIcon}><LuTarget size={16} /></span>
          <div className={styles.infoTextStack}>
            <span className={styles.infoLabel}>Hedef - Tarih Sütunu</span>
            <span className={styles.infoValue}>
              {dataset.targetColumn || '—'} - {dataset.dateColumn || '—'}
            </span>
          </div>
        </div>
      </div>

      {dataset.isProcessed && !dataset.errorMessage && (
        <DatasetStatisticsSection datasetId={datasetId} />
      )}
    </>
  );
};

export default DatasetSummary;