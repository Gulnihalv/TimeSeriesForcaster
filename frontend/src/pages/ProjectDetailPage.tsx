import { useState } from 'react';
import { useParams } from 'react-router-dom';
import Button from '../components/Button/Button';
import Modal from '../components/Modal/Modal';
import CreateDatasetForm from '../features/datasets/components/CreateDatasetForm';
import DatasetList from '../features/datasets/components/DatasetList';
import styles from './ProjectDetailPage.module.css'; 

const ProjectDetailPage = () => {
  const { projectId } = useParams();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [listKey, setListKey] = useState(0);
  const id = parseInt(projectId || '0');

  const handleDatasetCreated = () => {
    setIsModalOpen(false);
    setListKey(prevKey => prevKey + 1); // Listeyi yenilemeye zorla
  };

  if (id === 0) {
    return <div>Geçersiz Proje ID'si</div>;
  }

  return (
    <div className={styles.pageContainer}>
      <header className={styles.pageHeader}>
        <h1>Proje Detayı (ID: {id})</h1>
        <Button 
          onClick={() => setIsModalOpen(true)}
          style={{ width: 'auto' }}
        >
          Yeni Dataset Ekle
        </Button>
      </header>
      <DatasetList key={listKey} projectId={id} />
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Yeni Dataset Yükle"
      >
        <CreateDatasetForm 
          projectId={id} 
          onDatasetCreated={handleDatasetCreated} 
        />
      </Modal>
    </div>
  );
};

export default ProjectDetailPage;