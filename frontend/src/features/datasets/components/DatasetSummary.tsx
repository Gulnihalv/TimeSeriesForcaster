import { useEffect, type FC } from 'react';
import Card from '../../../components/Card/Card';
import Spinner from '../../../components/Spinner/Spinner';
import { ProcessingBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getDatasetById, type Dataset, type DataPoint } from '../api/datasetApi';
import { calculateStatistics, describeVariability } from '../utils/dataStatistics';
import styles from './DatasetSummary.module.css';

interface DatasetSummaryProps {
  datasetId: number;
  /** Dataset yüklendikten sonra parent'a (örn. ModelTrainingForm'un disable durumu için) haber verir */
  onLoaded?: (dataset: Dataset) => void;
  /** Ham veri noktaları - istatistik hesaplamak için DatasetDetailPage'den geliyor (tekrar fetch etmiyoruz) */
  dataPoints?: DataPoint[] | null;
  dataPointsLoading?: boolean;
}

const formatDate = (value: string | null) =>
  value ? new Date(value).toLocaleDateString() : '—';

const formatNumber = (value: number) =>
  value.toLocaleString('tr-TR', { maximumFractionDigits: 2 });

const DatasetSummary: FC<DatasetSummaryProps> = ({ datasetId, onLoaded, dataPoints, dataPointsLoading }) => {
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

  if (isLoading) return <Card>Dataset bilgisi yükleniyor...</Card>;
  if (error || !dataset) return <div className={styles.error}>{error || 'Dataset bulunamadı.'}</div>;

  const statistics = dataPoints ? calculateStatistics(dataPoints) : null;

  return (
    <Card>
      <div className={styles.header}>
        <div>
          <h2 className={styles.title}>{dataset.name}</h2>
          <p className={styles.fileName}>{dataset.originalFileName}</p>
        </div>
        <ProcessingBadge isProcessed={dataset.isProcessed} hasError={!!dataset.errorMessage} />
      </div>

      {dataset.errorMessage && (
        <div className={styles.error}>{dataset.errorMessage}</div>
      )}

      <div className={styles.statGrid}>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Kayıt sayısı</span>
          <span className={styles.statValue}>{dataset.recordCount ?? '—'}</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Tarih aralığı</span>
          <span className={styles.statValue}>
            {formatDate(dataset.startDate)} – {formatDate(dataset.endDate)}
          </span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Tarih sütunu</span>
          <span className={styles.statValue}>{dataset.dateColumn || '—'}</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Hedef sütun</span>
          <span className={styles.statValue}>{dataset.targetColumn || '—'}</span>
        </div>
      </div>

      {dataset.isProcessed && !dataset.errorMessage && (
        <div className={styles.statisticsSection}>
          <h4 className={styles.statisticsTitle}>İstatistikler</h4>
          {dataPointsLoading ? (
            <Spinner label="İstatistikler hesaplanıyor..." />
          ) : statistics ? (
            <div className={styles.statGrid}>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Ortalama</span>
                <span className={styles.statValue}>{formatNumber(statistics.mean)}</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Medyan</span>
                <span className={styles.statValue}>{formatNumber(statistics.median)}</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Std. sapma</span>
                <span className={styles.statValue}>{formatNumber(statistics.stdDev)}</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Değişkenlik</span>
                <span className={styles.statValue}>{describeVariability(statistics.coefficientOfVariation)}</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Min</span>
                <span className={styles.statValue}>
                  {formatNumber(statistics.min)} <small className={styles.statSub}>({formatDate(statistics.minDate)})</small>
                </span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Max</span>
                <span className={styles.statValue}>
                  {formatNumber(statistics.max)} <small className={styles.statSub}>({formatDate(statistics.maxDate)})</small>
                </span>
              </div>
            </div>
          ) : (
            <p className={styles.statisticsEmpty}>İstatistik hesaplanamadı.</p>
          )}
        </div>
      )}
    </Card>
  );
};

export default DatasetSummary;