import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import Card from '../components/Card/Card';
import Button from '../components/Button/Button';
import ProjectList from '../features/projects/components/ProjectList';
import RecentActivityFeed from '../features/dashboard/components/RecentActivityFeed';
import { useAuthStore } from '../store/authStore';
import { getProjects, type Project } from '../features/projects/api/projectApi';
import { useApiData } from '../hooks/useApiData';
import { LuFolderKanban, LuSparkles, LuTrendingUp } from 'react-icons/lu';
import styles from './DashboardPage.module.css';

const HeroChart = () => (
  <svg
    className={styles.heroChartSvg}
    viewBox="0 0 200 90"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <polyline
      points="0,70 30,55 60,62 90,35 120,42 150,15 180,25 200,5"
      stroke="url(#heroChartGradient)"
      strokeWidth="4"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
    <defs>
      <linearGradient id="heroChartGradient" x1="0" y1="0" x2="200" y2="0" gradientUnits="userSpaceOnUse">
        <stop offset="0" stopColor="#4B4D7A" />
        <stop offset="1" stopColor="#8B7CFF" />
      </linearGradient>
    </defs>
  </svg>
);

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
          <HeroChart />
        </div>
      </Card>

      <div className={styles.statRow}>
        <Card tone="violet" className={styles.statCard}>
          <div className={styles.statIcon}>
            <LuFolderKanban size={16} />
          </div>
          <div className={styles.statTexts}>
            <span className={styles.statValue}>{stats.total}</span>
            <span className={styles.statLabel}>Toplam proje</span>
          </div>
        </Card>

        <Card tone="green" className={styles.statCard}>
          <div className={styles.statIcon}>
            <LuTrendingUp size={16} />
          </div>
          <div className={styles.statTexts}>
            <span className={styles.statValue}>{stats.thisMonth}</span>
            <span className={styles.statLabel}>Bu ay oluşturulan</span>
          </div>
        </Card>

        <Card tone="amber" className={styles.statCard}>
          <div className={styles.statIcon}>
            <LuSparkles size={16} />
          </div>
          <div className={styles.statTexts}>
            <span className={styles.statValueSmall}>{stats.latestName}</span>
            <span className={styles.statLabel}>En son proje</span>
          </div>
        </Card>
      </div>

      <section className={styles.recentSection}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Son işlemler</h3>
        </div>
        <RecentActivityFeed />
      </section>

      <section className={styles.recentSection}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Son projeler</h3>
          <button className={styles.sectionLink} onClick={() => navigate('/projects')}>
            Tümünü gör
          </button>
        </div>
        <ProjectList limit={2} />
      </section>
    </div>
  );
};

export default DashboardPage;
