import { useState } from 'react';
import { getProjects, deleteProject, type Project } from '../api/projectApi';
import Card from '../../../components/Card/Card';
import EmptyState from '../../../components/EmptyState/EmptyState';
import Button from '../../../components/Button/Button';
import ConfirmDialog from '../../../components/ConfirmDialog/ConfirmDialog';
import styles from './ProjectList.module.css';
import { useNavigate } from 'react-router-dom';
import { useApiData } from '../../../hooks/useApiData';
import { getErrorMessage } from '../../../api/errorUtils';
import { useToast } from '../../../components/Toast/ToastContext';
import { TOAST_MESSAGES } from '../../../constants/messages';
import { LuFolderKanban, LuTrash2 } from 'react-icons/lu';

const TONES = ['violet', 'green', 'amber', 'blue', 'rose'] as const;

interface ProjectListProps {
  /** Verilirse yalnızca en yeni N proje gösterilir (Dashboard'daki "Son projeler" için). Ama şimdilik gerekli değil*/
  limit?: number;
}

const ProjectList = ({ limit }: ProjectListProps) => {
  const { data: projects, isLoading, error, refetch } = useApiData<Project[]>(
    getProjects,
    [],
    { fallbackErrorMessage: 'Projeler yüklenemedi.' }
  );
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [deleteTarget, setDeleteTarget] = useState<Project | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const handleCardClick = (projectId: number) => {
    navigate(`/projects/${projectId}`);
  };

  const handleDeleteClick = (e: React.MouseEvent, project: Project) => {
    e.stopPropagation(); // kartın kendi onClick'ini (navigasyonu) tetiklemesin
    setDeleteError(null);
    setDeleteTarget(project);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    setIsDeleting(true);
    setDeleteError(null);
    try {
      await deleteProject(deleteTarget.id);
      setDeleteTarget(null);
      refetch();
      showToast(TOAST_MESSAGES.projectDeleted, 'success');
    } catch (err) {
      setDeleteError(getErrorMessage(err, 'Proje silinemedi.'));
    } finally {
      setIsDeleting(false);
    }
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
    <>
      <div className={styles.listContainer}>
        {visible.map((project, index) => (
          <Card
            key={project.id}
            interactive
            onClick={() => handleCardClick(project.id)}
            className={styles.projectCard}
          >
            <div className={styles.cardHeader}>
              <div className={`${styles.iconBadge} ${styles[`tone_${TONES[index % TONES.length]}`]}`}>
                {project.name.slice(0, 1).toUpperCase()}
              </div>
              <button
                className={styles.deleteButton}
                onClick={(e) => handleDeleteClick(e, project)}
                title="Projeyi sil"
                aria-label="Projeyi sil"
              >
                <LuTrash2 size={16} />
              </button>
            </div>
            <h3 className={styles.projectName}>{project.name}</h3>
            <p className={styles.projectDesc}>{project.description || 'Açıklama yok'}</p>
            <small className={styles.projectDate}>
              Oluşturulma: {new Date(project.createdAt).toLocaleDateString('tr-TR')}
            </small>
          </Card>
        ))}
      </div>

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        title="Projeyi sil"
        message={`"${deleteTarget?.name}" projesini silmek istediğine emin misin? Bu projeye ait tüm dataset'ler ve modeller de erişilemez hale gelecek.`}
        isLoading={isDeleting}
        error={deleteError}
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
};

export default ProjectList;