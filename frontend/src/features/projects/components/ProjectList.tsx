import { getProjects, type Project } from '../api/projectApi';
import Card from '../../../components/Card/Card';
import EmptyState from '../../../components/EmptyState/EmptyState';
import Button from '../../../components/Button/Button';
import styles from './ProjectList.module.css';
import { useNavigate } from 'react-router-dom';
import { useApiData } from '../../../hooks/useApiData';
import { LuFolderKanban } from 'react-icons/lu';

const TONES = ['violet', 'green', 'amber', 'blue', 'rose'] as const;

interface ProjectListProps {
  /** Verilirse yalnızca en yeni N proje gösterilir (Dashboard'daki "Son projeler" için). */
  limit?: number;
}

const ProjectList = ({ limit }: ProjectListProps) => {
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
    return <div className={styles.loading}>Projeler yükleniyor...</div>;
  }
  if (error) {
    return <div className={styles.error}>{error}</div>;
  }

  const sorted = [...(projects ?? [])].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  );
  const visible = limit ? sorted.slice(0, limit) : sorted;

  if (visible.length === 0) {
    return (
      <EmptyState
        icon={<LuFolderKanban size={22} />}
        title="Henüz hiç projen yok"
        description="Bir proje oluşturarak veri setini yükleyebilir ve tahmin modelleri eğitebilirsin."
        action={
          <Button style={{ width: 'auto' }} onClick={() => navigate('/projects')}>
            Yeni Proje Oluştur
          </Button>
        }
      />
    );
  }

  return (
    <div className={styles.listContainer}>
      {visible.map((project, index) => (
        <Card
          key={project.id}
          interactive
          onClick={() => handleCardClick(project.id)}
          className={styles.projectCard}
        >
          <div className={`${styles.iconBadge} ${styles[`tone_${TONES[index % TONES.length]}`]}`}>
            {project.name.slice(0, 1).toUpperCase()}
          </div>
          <h3 className={styles.projectName}>{project.name}</h3>
          <p className={styles.projectDesc}>{project.description || 'Açıklama yok'}</p>
          <small className={styles.projectDate}>
            Oluşturulma: {new Date(project.createdAt).toLocaleDateString('tr-TR')}
          </small>
        </Card>
      ))}
    </div>
  );
};

export default ProjectList;
