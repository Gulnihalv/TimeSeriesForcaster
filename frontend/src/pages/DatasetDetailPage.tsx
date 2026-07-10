import { useCallback, useState } from 'react';
import { useParams } from 'react-router-dom';
import DatasetSummary from '../features/datasets/components/DatasetSummary';
import ModelTrainingForm from '../features/models/components/ModelTrainingForm';
import ModelList from '../features/models/components/ModelList';
import ModelDetailPanel from '../features/models/components/ModelDetailPanel';
import type { Dataset } from '../features/datasets/api/datasetApi';
import styles from './DatasetDetailPage.module.css';

const DatasetDetailPage = () => {
  const { datasetId } = useParams();
  const id = parseInt(datasetId || '0');

  const [dataset, setDataset] = useState<Dataset | null>(null);
  const [selectedModelId, setSelectedModelId] = useState<number | null>(null);
  const [modelListKey, setModelListKey] = useState(0);

  const handleDatasetLoaded = useCallback((loaded: Dataset) => {
    setDataset(loaded);
  }, []);

  const handleModelCreated = () => {
    setModelListKey((prev) => prev + 1);
  };

  if (id === 0) {
    return <div>Geçersiz Dataset ID'si</div>;
  }

  const trainingDisabled = !dataset || !dataset.isProcessed || !!dataset.errorMessage;
  const trainingDisabledReason = !dataset
    ? undefined
    : dataset.errorMessage
    ? 'Dataset işlenirken hata oluştu, model eğitilemez.'
    : !dataset.isProcessed
    ? 'Dataset henüz işlenmedi, model eğitimi için beklenmesi gerekiyor.'
    : undefined;

  return (
    <div className={styles.pageContainer}>
      <DatasetSummary datasetId={id} onLoaded={handleDatasetLoaded} />

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>Yeni model eğit</h3>
        <ModelTrainingForm
          datasetId={id}
          disabled={trainingDisabled}
          disabledReason={trainingDisabledReason}
          onModelCreated={handleModelCreated}
        />
      </section>

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>Eğitilen modeller</h3>
        <ModelList
          key={modelListKey}
          datasetId={id}
          selectedModelId={selectedModelId}
          onSelectModel={setSelectedModelId}
        />
      </section>

      {selectedModelId !== null && (
        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>Model detayı</h3>
          <ModelDetailPanel modelId={selectedModelId} />
        </section>
      )}
    </div>
  );
};

export default DatasetDetailPage;
