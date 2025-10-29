import { useState } from 'react';
import ProjectList from '../features/projects/components/ProjectList';
import CreateProjectForm from '../features/projects/components/CreateProjectForm';
import Modal from '../components/Modal/Modal';
import Button from '../components/Button/Button';
import styles from './ProjectsPage.module.css';

const ProjectsPage = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [projectListKey, setProjectListKey] = useState(0);

  const handleProjectCreated = () => {
    setIsModalOpen(false) // Modalı kapamak için
    setProjectListKey(prevKey => prevKey + 1); // Listeyi yenilemek için
  };

  return (
    <div className={styles.projectsContainer}>
      <header className={styles.header}>   
          <div className={styles.headerButtons}>
            <Button 
              onClick={() => setIsModalOpen(true)}
              style={{ width: 'auto' }}
            >
              Yeni Proje Oluştur
            </Button>
          </div>
      </header>
      {/* Proje Listesi */}
      <ProjectList key={projectListKey} />
      {/* Yeni Proje Modalı */}
      <Modal 
        isOpen={isModalOpen} 
        onClose={() => setIsModalOpen(false)}
        title="Yeni Proje Oluştur"
      >
      <CreateProjectForm onProjectCreated={handleProjectCreated} />
      </Modal>
    </div>
  );
};

export default ProjectsPage;