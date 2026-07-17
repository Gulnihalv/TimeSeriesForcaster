import { useCallback, useState, type FC } from 'react';
import Card from '../../../components/Card/Card';
import ConfirmDialog from '../../../components/ConfirmDialog/ConfirmDialog';
import { StatusBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getModelsForDataset, deleteModel, ModelStatus, type Model } from '../api/modelApi';
import { getErrorMessage } from '../../../api/errorUtils';
import { LuTrash2 } from 'react-icons/lu';
import styles from './ModelList.module.css';

const MAX_COMPARISON = 4;

interface ModelListProps {
  datasetId: number;
  selectedModelIds: number[];
  onSelectionChange: (modelIds: number[]) => void;
  /** Seçili model silinirse üst bileşenin seçimi güncelleyebilmesi için */
  onModelDeleted?: (modelId: number) => void;
}

const isActive = (status: ModelStatus) =>
  status === ModelStatus.Queued || status === ModelStatus.Training;

const ModelList: FC<ModelListProps> = ({ datasetId, selectedModelIds, onSelectionChange, onModelDeleted }) => {
  const shouldPoll = useCallback(
    (models: Model[]) => models.some((m) => isActive(m.status)),
    []
  );

  const { data: models, isLoading, error, refetch } = useApiData<Model[]>(
    () => getModelsForDataset(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'Modeller yüklenemedi.', shouldPoll, pollIntervalMs: 3000 }
  );

  const [compareMode, setCompareMode] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Model | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [limitHint, setLimitHint] = useState(false);

  const toggleCompareMode = () => {
    setCompareMode((prev) => !prev);
    onSelectionChange([]); // mod değişince seçimi sıfırla, karışıklığı önlemek için
  };

  const handleCardClick = (modelId: number) => {
    if (!compareMode) {
      onSelectionChange([modelId]);
      return;
    }

    if (selectedModelIds.includes(modelId)) {
      onSelectionChange(selectedModelIds.filter((id) => id !== modelId));
      return;
    }

    if (selectedModelIds.length >= MAX_COMPARISON) {
      setLimitHint(true);
      setTimeout(() => setLimitHint(false), 2500);
      return;
    }

    onSelectionChange([...selectedModelIds, modelId]);
  };

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

  return (
    <>
      <div className={styles.toolbar}>
        <button
          type="button"
          className={`${styles.compareToggle} ${compareMode ? styles.compareToggleActive : ''}`}
          onClick={toggleCompareMode}
        >
          {compareMode ? 'Karşılaştırmayı kapat' : 'Karşılaştır'}
        </button>
        {compareMode && (
          <span className={styles.compareHint}>
            {limitHint
              ? `En fazla ${MAX_COMPARISON} model seçebilirsin.`
              : `${selectedModelIds.length} model seçili (2-${MAX_COMPARISON} arası seç)`}
          </span>
        )}
      </div>

      {(!models || models.length === 0) ? (
        <p className={styles.empty}>Bu dataset için henüz eğitilmiş bir model yok.</p>
      ) : (
        <div className={styles.list}>
          {models.map((model) => {
            const isSelected = selectedModelIds.includes(model.id);
            return (
              <Card
                key={model.id}
                interactive
                onClick={() => handleCardClick(model.id)}
                className={`${styles.modelCard} ${isSelected ? styles.selected : ''}`}
              >
                <div className={styles.row}>
                  <div className={styles.rowMain}>
                    {compareMode && (
                      <input
                        type="checkbox"
                        checked={isSelected}
                        readOnly
                        className={styles.checkbox}
                        aria-label={`${model.modelName} karşılaştırmaya ekle`}
                      />
                    )}
                    <div>
                      <p className={styles.name}>{model.modelName || model.algorithm}</p>
                      <p className={styles.meta}>
                        {model.trainingStartedAt
                          ? `Başladı: ${new Date(model.trainingStartedAt).toLocaleString()}`
                          : 'Henüz başlamadı'}
                      </p>
                    </div>
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
            );
          })}
        </div>
      )}

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