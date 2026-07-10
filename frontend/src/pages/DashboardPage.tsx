import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import Card from '../components/Card/Card';
import Button from '../components/Button/Button';
import ProjectList from '../features/projects/components/ProjectList';
import { useAuthStore } from '../store/authStore';
import { getProjects, type Project } from '../features/projects/api/projectApi';
import { useApiData } from '../hooks/useApiData';
import { LuFolderKanban, LuSparkles, LuTrendingUp } from 'react-icons/lu';
import styles from './DashboardPage.module.css';

const isSameMonth = (iso: string) => {
  const d = new Date(iso);
  const now = new Date();
  return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
};

const DashboardPage = () => {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);

  const { data: projects } = useApiData<Project[]>(getProjects, []);

  const stats = useMemo(() => {
    const list = projects ?? [];
    const thisMonth = list.filter((p) => isSameMonth(p.createdAt)).length;
    const latest = [...list].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )[0];
    return {
      total: list.length,
      thisMonth,
      latestName: latest?.name ?? '—',
    };
  }, [projects]);

  return (
    <div className={styles.dashboardGrid}>
      <Card tone="dark" className={styles.heroCard}>
        <div className={styles.heroText}>
          <span className={styles.heroEyebrow}>Hoş geldin{user?.firstName ? `, ${user.firstName}` : ''}</span>
          <h2 className={styles.heroTitle}>Zaman serisi tahminlerini tek yerden yönet</h2>
          <p className={styles.heroDescription}>
            Verini yükle, modelini eğit ve tahminlerini incele. Yeni bir proje
            oluşturarak başlayabilirsin.
          </p>
          <Button
            variant="white"
            className={styles.heroButton}
            onClick={() => navigate('/projects')}
          >
            Yeni Proje Oluştur
          </Button>
        </div>
        <div className={styles.heroIcon}>
          <LuSparkles size={64} />
        </div>
      </Card>

      <div className={styles.statRow}>
        <Card tone="violet" className={styles.statCard}>
          <div className={styles.statIcon}>
            <LuFolderKanban size={20} />
          </div>
          <span className={styles.statValue}>{stats.total}</span>
          <span className={styles.statLabel}>Toplam proje</span>
        </Card>

        <Card tone="green" className={styles.statCard}>
          <div className={styles.statIcon}>
            <LuTrendingUp size={20} />
          </div>
          <span className={styles.statValue}>{stats.thisMonth}</span>
          <span className={styles.statLabel}>Bu ay oluşturulan</span>
        </Card>

        <Card tone="amber" className={styles.statCard}>
          <div className={styles.statIcon}>
            <LuSparkles size={20} />
          </div>
          <span className={styles.statValueSmall}>{stats.latestName}</span>
          <span className={styles.statLabel}>En son proje</span>
        </Card>
      </div>

      <section className={styles.recentSection}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Son projeler</h3>
          <button className={styles.sectionLink} onClick={() => navigate('/projects')}>
            Tümünü gör
          </button>
        </div>
        <ProjectList limit={3} />
      </section>
    </div>
  );
};

export default DashboardPage;
