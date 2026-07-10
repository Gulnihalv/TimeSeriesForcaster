import { useEffect, type FC } from 'react';
import Card from '../../../components/Card/Card';
import { ProcessingBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getDatasetById, type Dataset } from '../api/datasetApi';
import styles from './DatasetSummary.module.css';

interface DatasetSummaryProps {
  datasetId: number;
  /** Dataset yüklendikten sonra parent'a (örn. ModelTrainingForm'un disable durumu için) haber verir */
  onLoaded?: (dataset: Dataset) => void;
}

const formatDate = (value: string | null) =>
  value ? new Date(value).toLocaleDateString() : '—';

const DatasetSummary: FC<DatasetSummaryProps> = ({ datasetId, onLoaded }) => {
  const { data: dataset, isLoading, error } = useApiData<Dataset>(
    () => getDatasetById(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'Dataset bilgisi yüklenemedi.' }
  );

  useEffect(() => {
    if (dataset && onLoaded) onLoaded(dataset);
  }, [dataset, onLoaded]);

  if (isLoading) return <Card>Dataset bilgisi yükleniyor...</Card>;
  if (error || !dataset) return <div className={styles.error}>{error || 'Dataset bulunamadı.'}</div>;

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
    </Card>
  );
};

export default DatasetSummary;
