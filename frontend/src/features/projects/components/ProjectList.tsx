import { getProjects, type Project } from '../api/projectApi';
import Card from '../../../components/Card/Card';
import styles from './ProjectList.module.css';
import { useNavigate } from 'react-router-dom';
import { useApiData } from '../../../hooks/useApiData';

const ProjectList = () => {
  const { data: projects, isLoading, error } = useApiData<Project[]>(
    getProjects,
    [],
    { fallbackErrorMessage: 'Projeler yüklenemedi.' }
  );
  const navigate = useNavigate();

  const handleCardClick = (projectId: number) => {
    navigate(`/projects/${projectId}`);
  };

  if (isLoading) {
    return <div>Yükleniyor...</div>;
  }
  if (error) {
    return <div className={styles.error}>{error}</div>;
  }
  return (
    <div className={styles.listContainer}>
      {!projects || projects.length === 0 ? (
        <p>Henüz hiç projeniz yok. Yeni bir tane oluşturun!</p>
      ) : (
        projects.map((project) => (
          <Card
            key={project.id}
            interactive
            onClick={() => handleCardClick(project.id)}
          >
            <div className={styles.projectCard}>
              <h3>{project.name}</h3>
              <p>{project.description || 'Açıklama yok'}</p>
              <small>Oluşturulma: {new Date(project.createdAt).toLocaleDateString()}</small>
            </div>
          </Card>
        ))
      )}
    </div>
  );
};

export default ProjectList;
