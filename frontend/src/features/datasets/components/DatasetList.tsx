import { useNavigate } from 'react-router-dom';
import type { FC } from 'react';
import { getDatasetsForProject, type Dataset } from '../api/datasetApi';
import Card from '../../../components/Card/Card';
import EmptyState from '../../../components/EmptyState/EmptyState';
import { ProcessingBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import { LuDatabase } from 'react-icons/lu';
import styles from './DatasetList.module.css';

interface DatasetListProps {
  projectId: number;
}

const DatasetList: FC<DatasetListProps> = ({ projectId }) => {
  const { data: datasets, isLoading, error } = useApiData<Dataset[]>(
    () => getDatasetsForProject(projectId),
    [projectId],
    {
      fallbackErrorMessage: 'Datasetler yüklenemedi.',
      shouldPoll: (list) => list.some((d) => !d.isProcessed && !d.errorMessage),
    }
  );
  const navigate = useNavigate();

  const handleCardClick = (datasetId: number) => {
    navigate(`/datasets/${datasetId}`);
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
    <div className={styles.listContainer}>
      {datasets.map((dataset) => (
        <Card
          key={dataset.id}
          interactive
          onClick={() => handleCardClick(dataset.id)}
          className={styles.datasetCard}
        >
          <div className={styles.datasetCardHeader}>
            <div className={styles.iconBadge}>
              <LuDatabase size={18} />
            </div>
            <ProcessingBadge isProcessed={dataset.isProcessed} hasError={!!dataset.errorMessage} />
          </div>
          <h3 className={styles.datasetName}>{dataset.name}</h3>
          <p className={styles.datasetFile}>{dataset.originalFileName}</p>
          <small className={styles.datasetDate}>
            Yüklenme: {new Date(dataset.createdAt).toLocaleString('tr-TR')}
          </small>
        </Card>
      ))}
    </div>
  );
};

export default DatasetList;