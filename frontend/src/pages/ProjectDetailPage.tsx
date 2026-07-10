import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LuArrowLeft } from 'react-icons/lu';
import Button from '../components/Button/Button';
import Modal from '../components/Modal/Modal';
import CreateDatasetForm from '../features/datasets/components/CreateDatasetForm';
import DatasetList from '../features/datasets/components/DatasetList';
import styles from './ProjectDetailPage.module.css';

const ProjectDetailPage = () => {
  const { projectId } = useParams();
  const navigate = useNavigate();
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
        <button className={styles.backLink} onClick={() => navigate('/projects')}>
          <LuArrowLeft size={16} /> Tüm projeler
        </button>
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