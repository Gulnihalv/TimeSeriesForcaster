import { useNavigate } from 'react-router-dom';
import type { FC } from 'react';
import { getDatasetsForProject, type Dataset } from '../api/datasetApi';
import Card from '../../../components/Card/Card';
import { ProcessingBadge } from '../../../components/StatusBadge/StatusBadge';
import { useApiData } from '../../../hooks/useApiData';
import styles from './DatasetList.module.css';

interface DatasetListProps {
  projectId: number;
}

const DatasetList: FC<DatasetListProps> = ({ projectId }) => {
  const { data: datasets, isLoading, error } = useApiData<Dataset[]>(
    () => getDatasetsForProject(projectId),
    [projectId],
    { fallbackErrorMessage: 'Datasetler yüklenemedi.' }
  );
  const navigate = useNavigate();

  const handleCardClick = (datasetId: number) => {
    navigate(`/datasets/${datasetId}`);
  };

  if (isLoading) return <div>Datasetler Yükleniyor...</div>;
  if (error) return <div className={styles.error}>{error}</div>;

  return (
    <div className={styles.listContainer}>
      {!datasets || datasets.length === 0 ? (
        <p>Bu projeye ait hiç dataset yok. Yeni bir tane yükleyin!</p>
      ) : (
        datasets.map((dataset) => (
          <Card
            key={dataset.id}
            interactive
            onClick={() => handleCardClick(dataset.id)}
          >
            <div className={styles.datasetCardHeader}>
              <h3>{dataset.name}</h3>
              <ProcessingBadge isProcessed={dataset.isProcessed} hasError={!!dataset.errorMessage} />
            </div>
            <p>Dosya Adı: {dataset.originalFileName}</p>
            <small>Yüklenme: {new Date(dataset.createdAt).toLocaleString()}</small>
          </Card>
        ))
      )}
    </div>
  );
};

export default DatasetList;
