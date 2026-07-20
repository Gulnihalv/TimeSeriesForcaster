import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { FC } from 'react';
import { getDatasetsForProject, deleteDataset, type Dataset } from '../api/datasetApi';
import Card from '../../../components/Card/Card';
import EmptyState from '../../../components/EmptyState/EmptyState';
import ConfirmDialog from '../../../components/ConfirmDialog/ConfirmDialog';
import { ProcessingBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { getErrorMessage } from '../../../api/errorUtils';
import { LuDatabase, LuTrash2 } from 'react-icons/lu';
import styles from './DatasetList.module.css';

interface DatasetListProps {
  projectId: number;
}

const DatasetList: FC<DatasetListProps> = ({ projectId }) => {
  const { data: datasets, isLoading, error, refetch } = useApiData<Dataset[]>(
    () => getDatasetsForProject(projectId),
    [projectId],
    {
      fallbackErrorMessage: 'Datasetler yüklenemedi.',
      shouldPoll: (list) => list.some((d) => !d.isProcessed && !d.errorMessage),
    }
  );
  const navigate = useNavigate();

  const [deleteTarget, setDeleteTarget] = useState<Dataset | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [notReadyHintId, setNotReadyHintId] = useState<number | null>(null);

  const handleCardClick = (dataset: Dataset) => {
    // TODO: gerçek toast sistemi kurulunca (Gün 18-19) bu satır-içi uyarıyı ona bağla
    if (!dataset.isProcessed && !dataset.errorMessage) {
      setNotReadyHintId(dataset.id);
      setTimeout(() => setNotReadyHintId((current) => (current === dataset.id ? null : current)), 2500);
      return;
    }
    navigate(`/datasets/${dataset.id}`);
  };

  const handleDeleteClick = (e: React.MouseEvent, dataset: Dataset) => {
    e.stopPropagation();
    setDeleteError(null);
    setDeleteTarget(dataset);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    setIsDeleting(true);
    setDeleteError(null);
    try {
      await deleteDataset(deleteTarget.id);
      setDeleteTarget(null);
      refetch();
    } catch (err) {
      setDeleteError(getErrorMessage(err, 'Dataset silinemedi.'));
    } finally {
      setIsDeleting(false);
    }
  };

  if (isLoading) return <div className={styles.loading}>Datasetler yükleniyor...</div>;
  if (error) return <div className={styles.error}>{error}</div>;

  if (!datasets || datasets.length === 0) {
    return (
      <EmptyState
        icon={<LuDatabase size={22} />}
        title="Bu projeye ait henüz dataset yok"
        description="Sağ üstteki butondan bir CSV dosyası yükleyerek ilk datasetini oluşturabilirsin."
      />
    );
  }

  return (
    <>
      <div className={styles.listContainer}>
        {datasets.map((dataset) => (
          <Card
            key={dataset.id}
            interactive
            onClick={() => handleCardClick(dataset)}
            className={styles.datasetCard}
          >
            <div className={styles.datasetCardHeader}>
              <div className={styles.iconBadge}>
                <LuDatabase size={18} />
              </div>
              <div className={styles.headerActions}>
                <ProcessingBadge isProcessed={dataset.isProcessed} hasError={!!dataset.errorMessage} />
                <button
                  className={styles.deleteButton}
                  onClick={(e) => handleDeleteClick(e, dataset)}
                  title="Dataset'i sil"
                  aria-label="Dataset'i sil"
                >
                  <LuTrash2 size={16} />
                </button>
              </div>
            </div>
            <h3 className={styles.datasetName}>{dataset.name}</h3>
            <p className={styles.datasetFile}>{dataset.originalFileName}</p>
            <small className={styles.datasetDate}>
              Yüklenme: {new Date(dataset.createdAt).toLocaleString('tr-TR')}
            </small>
            {notReadyHintId === dataset.id && (
              <p className={styles.notReadyHint}>Dataset henüz işleniyor, birkaç saniye sonra tekrar dene.</p>
            )}
          </Card>
        ))}
      </div>

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        title="Dataset'i sil"
        message={`"${deleteTarget?.name}" dataset'ini silmek istediğine emin misin? Bu dataset'e bağlı tüm modeller de erişilemez hale gelecek.`}
        isLoading={isDeleting}
        error={deleteError}
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
};

export default DatasetList;