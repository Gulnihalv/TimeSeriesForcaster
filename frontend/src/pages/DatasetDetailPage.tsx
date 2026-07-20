import { useCallback, useState } from 'react';
import { useParams } from 'react-router-dom';
import Card from '../components/Card/Card';
import DatasetSummary from '../features/datasets/components/DatasetSummary';
import DatasetChart from '../features/datasets/components/DatasetChart';
import ModelTrainingForm from '../features/models/components/ModelTrainingForm';
import ModelList from '../features/models/components/ModelList';
import ModelDetailPanel from '../features/models/components/ModelDetailPanel';
import ModelComparisonView from '../features/models/components/ModelComparisonView';
import EmptyState from '../components/EmptyState/EmptyState';
import Spinner from '../components/Spinner/Spinner';
import { useApiData } from '../hooks/useApiData';
import { getDataPointsForDataset, type Dataset, type DataPoint } from '../features/datasets/api/datasetApi';
import { LuChartSpline } from 'react-icons/lu';
import styles from './DatasetDetailPage.module.css';

const DatasetDetailPage = () => {
  const { datasetId } = useParams();
  const id = parseInt(datasetId || '0');

  const [dataset, setDataset] = useState<Dataset | null>(null);
  const [selectedModelIds, setSelectedModelIds] = useState<number[]>([]);
  const [modelListKey, setModelListKey] = useState(0);

  const handleDatasetLoaded = useCallback((loaded: Dataset) => {
    setDataset(loaded);
  }, []);

  // Dataset işlendikten sonra ham veri noktalarını TEK SEFERDE çekip hem
  // DatasetSummary (istatistikler) hem DatasetChart (grafik) ile paylaşıyoruz -
  // ikisi de kendi ayrı fetch'ini yapmıyor.
  const { data: dataPoints, isLoading: pointsLoading, error: pointsError } = useApiData<DataPoint[]>(
    () => (dataset?.isProcessed ? getDataPointsForDataset(id) : Promise.resolve([])),
    [id, dataset?.isProcessed],
    { fallbackErrorMessage: 'Veri noktaları yüklenemedi.' }
  );

  const handleModelCreated = () => {
    setModelListKey((prev) => prev + 1);
  };

  const handleModelDeleted = (deletedModelId: number) => {
    setSelectedModelIds((current) => current.filter((mid) => mid !== deletedModelId));
  };

  const handleRemoveFromComparison = (modelId: number) => {
    setSelectedModelIds((current) => current.filter((mid) => mid !== modelId));
  };

  if (id === 0 || Number.isNaN(id)) {
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
    <div>
      {/* Üst hero bölümü: dataset özeti (+ istatistikler) + ham veri grafiği, koyu çerçeve kart içinde */}
      <Card tone="dark" className={styles.heroWrapper}>
        <div className={styles.heroRow}>
          <DatasetSummary
            datasetId={id}
            onLoaded={handleDatasetLoaded}
            dataPoints={dataPoints}
            dataPointsLoading={pointsLoading}
          />

          <Card className={styles.chartCard}>
            <h3 className={styles.sectionTitle}>Ham veri grafiği</h3>
            {!dataset?.isProcessed ? (
              <p className={styles.chartPlaceholder}>
                {dataset?.errorMessage
                  ? 'Dataset işlenirken hata oluştu, grafik gösterilemiyor.'
                  : 'Dataset işlendikten sonra grafik burada görünecek.'}
              </p>
            ) : pointsLoading ? (
              <Spinner label="Veriler yükleniyor..." />
            ) : pointsError ? (
              <p className={styles.chartPlaceholder}>{pointsError}</p>
            ) : (
              <DatasetChart dataPoints={dataPoints ?? []} />
            )}
          </Card>
        </div>
      </Card>

      <div className={styles.pageContainer}>
        {/* Sol rail: eğitim formu, model listesi */}
        <div className={styles.rail}>
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
              selectedModelIds={selectedModelIds}
              onSelectionChange={setSelectedModelIds}
              onModelDeleted={handleModelDeleted}
            />
          </Card>
        </div>

        {/* Sağ: seçime göre tek model detayı ya da çoklu model karşılaştırması */}
        <div className={styles.detailColumn}>
          {selectedModelIds.length === 0 && (
            <div className={styles.emptyWrap}>
              <EmptyState
                icon={<LuChartSpline size={22} />}
                title="Henüz bir model seçilmedi"
                description="Metriklerini ve tahmin grafiğini görmek için soldaki listeden bir model seç. Karşılaştırmak için 'Karşılaştır' modunu açıp birden fazla model seçebilirsin."
              />
            </div>
          )}

          {selectedModelIds.length === 1 && (
            <ModelDetailPanel modelId={selectedModelIds[0]} />
          )}

          {selectedModelIds.length >= 2 && (
            <Card>
              <ModelComparisonView
                modelIds={selectedModelIds}
                onRemoveModel={handleRemoveFromComparison}
              />
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

export default DatasetDetailPage;