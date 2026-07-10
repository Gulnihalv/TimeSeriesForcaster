import { useCallback, useState, type FC } from 'react';
import Card from '../../../components/Card/Card';
import ConfirmDialog from '../../../components/ConfirmDialog/ConfirmDialog';
import { StatusBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getModelsForDataset, deleteModel, ModelStatus, type Model } from '../api/modelApi';
import { getErrorMessage } from '../../../api/errorUtils';
import { LuTrash2 } from 'react-icons/lu';
import styles from './ModelList.module.css';

interface ModelListProps {
  datasetId: number;
  selectedModelId: number | null;
  onSelectModel: (modelId: number) => void;
  /** Seçili model silinirse üst bileşenin seçimi temizleyebilmesi için */
  onModelDeleted?: (modelId: number) => void;
}

const isActive = (status: ModelStatus) =>
  status === ModelStatus.Queued || status === ModelStatus.Training;

const ModelList: FC<ModelListProps> = ({ datasetId, selectedModelId, onSelectModel, onModelDeleted }) => {
  const shouldPoll = useCallback(
    (models: Model[]) => models.some((m) => isActive(m.status)),
    []
  );

  const { data: models, isLoading, error, refetch } = useApiData<Model[]>(
    () => getModelsForDataset(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'Modeller yüklenemedi.', shouldPoll, pollIntervalMs: 3000 }
  );

  const [deleteTarget, setDeleteTarget] = useState<Model | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const handleDeleteClick = (e: React.MouseEvent, model: Model) => {
    e.stopPropagation();
    setDeleteError(null);
    setDeleteTarget(model);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    setIsDeleting(true);
    setDeleteError(null);
    try {
      await deleteModel(deleteTarget.id);
      const deletedId = deleteTarget.id;
      setDeleteTarget(null);
      refetch();
      if (onModelDeleted) onModelDeleted(deletedId);
    } catch (err) {
      setDeleteError(getErrorMessage(err, 'Model silinemedi.'));
    } finally {
      setIsDeleting(false);
    }
  };

  if (isLoading) return <div>Modeller yükleniyor...</div>;
  if (error) return <div className={styles.error}>{error}</div>;

  if (!models || models.length === 0) {
    return <p className={styles.empty}>Bu dataset için henüz eğitilmiş bir model yok.</p>;
  }

  return (
    <>
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
              <div className={styles.rowActions}>
                <StatusBadge status={model.status} />
                <button
                  className={styles.deleteButton}
                  onClick={(e) => handleDeleteClick(e, model)}
                  title="Modeli sil"
                  aria-label="Modeli sil"
                >
                  <LuTrash2 size={16} />
                </button>
              </div>
            </div>
          </Card>
        ))}
      </div>

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        title="Modeli sil"
        message={`"${deleteTarget?.modelName || deleteTarget?.algorithm}" modelini silmek istediğine emin misin? Bu modele ait tüm tahminler de silinecek.`}
        isLoading={isDeleting}
        error={deleteError}
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
};

export default ModelList;