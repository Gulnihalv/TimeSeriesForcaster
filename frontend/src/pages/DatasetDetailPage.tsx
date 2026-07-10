import { useCallback, useState } from 'react';
import { useParams } from 'react-router-dom';
import Card from '../components/Card/Card';
import DatasetSummary from '../features/datasets/components/DatasetSummary';
import ModelTrainingForm from '../features/models/components/ModelTrainingForm';
import ModelList from '../features/models/components/ModelList';
import ModelDetailPanel from '../features/models/components/ModelDetailPanel';
import EmptyState from '../components/EmptyState/EmptyState';
import type { Dataset } from '../features/datasets/api/datasetApi';
import { LuChartSpline } from 'react-icons/lu';
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

  const handleModelDeleted = (deletedModelId: number) => {
    setSelectedModelId((current) => (current === deletedModelId ? null : current));
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
      {/* Sol rail: dataset özeti (ayrı kart), eğitim formu, model listesi */}
      <div className={styles.rail}>
        <DatasetSummary datasetId={id} onLoaded={handleDatasetLoaded} />

        <Card className={styles.section}>
          <h3 className={styles.sectionTitle}>Yeni model eğit</h3>
          <ModelTrainingForm
            datasetId={id}
            disabled={trainingDisabled}
            disabledReason={trainingDisabledReason}
            onModelCreated={handleModelCreated}
          />
        </Card>

        <Card className={styles.section}>
          <h3 className={styles.sectionTitle}>Eğitilen modeller</h3>
          <ModelList
            key={modelListKey}
            datasetId={id}
            selectedModelId={selectedModelId}
            onSelectModel={setSelectedModelId}
            onModelDeleted={handleModelDeleted}
          />
        </Card>
      </div>

      {/* Sağ: seçili modelin detayı, geniş alanı kullanır */}
      <div className={styles.detailColumn}>
        {selectedModelId !== null ? (
          <ModelDetailPanel modelId={selectedModelId} />
        ) : (
          <div className={styles.emptyWrap}>
            <EmptyState
              icon={<LuChartSpline size={22} />}
              title="Henüz bir model seçilmedi"
              description="Metriklerini ve tahmin grafiğini görmek için soldaki listeden bir model seç."
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default DatasetDetailPage;