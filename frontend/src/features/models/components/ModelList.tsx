import { useCallback, type FC } from 'react';
import Card from '../../../components/Card/Card';
import { StatusBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getModelsForDataset, ModelStatus, type Model } from '../api/modelApi';
import styles from './ModelList.module.css';

interface ModelListProps {
  datasetId: number;
  selectedModelId: number | null;
  onSelectModel: (modelId: number) => void;
}

const isActive = (status: ModelStatus) =>
  status === ModelStatus.Queued || status === ModelStatus.Training;

const ModelList: FC<ModelListProps> = ({ datasetId, selectedModelId, onSelectModel }) => {
  const shouldPoll = useCallback(
    (models: Model[]) => models.some((m) => isActive(m.status)),
    []
  );

  const { data: models, isLoading, error } = useApiData<Model[]>(
    () => getModelsForDataset(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'Modeller yüklenemedi.', shouldPoll, pollIntervalMs: 3000 }
  );

  if (isLoading) return <div>Modeller yükleniyor...</div>;
  if (error) return <div className={styles.error}>{error}</div>;

  if (!models || models.length === 0) {
    return <p className={styles.empty}>Bu dataset için henüz eğitilmiş bir model yok.</p>;
  }

  return (
    <div className={styles.list}>
      {models.map((model) => (
        <Card
          key={model.id}
          interactive
          onClick={() => onSelectModel(model.id)}
          className={`${styles.modelCard} ${model.id === selectedModelId ? styles.selected : ''}`}
        >
          <div className={styles.row}>
            <div>
              <p className={styles.name}>{model.modelName || model.algorithm}</p>
              <p className={styles.meta}>
                {model.trainingStartedAt
                  ? `Başladı: ${new Date(model.trainingStartedAt).toLocaleString()}`
                  : 'Henüz başlamadı'}
              </p>
            </div>
            <StatusBadge status={model.status} />
          </div>
        </Card>
      ))}
    </div>
  );
};

export default ModelList;
