import { useState, useEffect } from 'react';
import { getProjects, type Project } from '../api/projectApi';
import Card from '../../../components/Card/Card';
import styles from './ProjectList.module.css';

const ProjectList = () => {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchProjects = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await getProjects();
        setProjects(data);
        setIsLoading(false);
      } catch (err) {
        setError('Projeler yüklenemedi.');
        setIsLoading(false);
        console.error(err);
      }
    };

    fetchProjects();
  }, []);

  if (isLoading) {
    return <div>Yükleniyor...</div>;
  }
  if (error) {
    return <div className={styles.error}>{error}</div>;
  }
  return (
    <div className={styles.listContainer}>
      {projects.length === 0 ? (
        <p>Henüz hiç projeniz yok. Yeni bir tane oluşturun!</p>
      ) : (
        projects.map((project) => (
          <Card key={project.id}>
            <div className={styles.projectCard}>
              <h3>{project.name}</h3>
              <p>{project.description || 'Açıklama yok'}</p>
              <small>Oluşturulma: {new Date(project.createdAt).toLocaleDateString()}</small>
              {/* Sonra proje detayına gitme ekleneicek*/}
            </div>
          </Card>
        ))
      )}
    </div>
  );
};

export default ProjectList;