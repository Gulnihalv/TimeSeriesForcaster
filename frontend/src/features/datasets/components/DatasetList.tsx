import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getDatasetsForProject, type Dataset } from '../api/datasetApi';
import Card from '../../../components/Card/Card';
import styles from './DatasetList.module.css';

interface DatasetListProps {
  projectId: number;
}

const DatasetList: React.FC<DatasetListProps> = ({ projectId }) => {
  const [datasets, setDatasets] = useState<Dataset[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchDatasets = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await getDatasetsForProject(projectId);
        setDatasets(data);
        setIsLoading(false);
      } catch (err) {
        setError('Datasetler yüklenemedi.');
        setIsLoading(false);
        console.error(err);
      }
    };

    fetchDatasets();
  }, [projectId]);

  const handleCardClick = (datasetId: number) => {
    navigate(`/datasets/${datasetId}`);
  };

  if (isLoading) return <div>Datasetler Yükleniyor...</div>;
  if (error) return <div className={styles.error}>{error}</div>;

  return (
    <div className={styles.listContainer}>
      {datasets.length === 0 ? (
        <p>Bu projeye ait hiç dataset yok. Yeni bir tane yükleyin!</p>
      ) : (
        datasets.map((dataset) => (
          <Card 
            key={dataset.id} 
            onClick={() => handleCardClick(dataset.id)}
            className={styles.datasetCardWrapper}
          >
            <h3>{dataset.name}</h3>
            <p>Dosya Adı: {dataset.originalFileName}</p>
            <p>Durum: {dataset.isProcessed ? 'İşlendi' : 'İşlenmedi'}</p>
            <small>Yüklenme: {new Date(dataset.createdAt).toLocaleString()}</small>
          </Card>
        ))
      )}
    </div>
  );
};

export default DatasetList;