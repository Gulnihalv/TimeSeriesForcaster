import { Outlet, useLocation } from 'react-router-dom';
import Sidebar from './Sidebar';
import { useAuthStore } from '../../store/authStore';
import { LuBell, LuSearch } from 'react-icons/lu';
import styles from './MainLayout.module.css';

const PAGE_META: { pattern: string; title: string; subtitle: string }[] = [
  { pattern: '/dashboard', title: 'Ana Sayfa', subtitle: 'Projelerine genel bir bakış' },
  { pattern: '/projects', title: 'Projeler', subtitle: 'Tüm projelerini buradan yönet' },
  { pattern: '/projects/:projectId', title: 'Proje Detayı', subtitle: 'Bu projeye ait datasetler' },
  { pattern: '/datasets/:datasetId', title: 'Dataset Detayı', subtitle: 'Model eğit ve tahmin oluştur' },
  { pattern: '/settings', title: 'Ayarlar', subtitle: 'Hesap ve uygulama tercihlerin' },
];

const usePageMeta = () => {
  const location = useLocation();
  for (const meta of PAGE_META) {
    // useMatch tek bir pattern için çalıştığından burada basit bir kontrol yeterli
    if (matchPattern(meta.pattern, location.pathname)) return meta;
  }
  return { title: 'Genel Bakış', subtitle: '' };
};

const matchPattern = (pattern: string, pathname: string) => {
  const patternParts = pattern.split('/').filter(Boolean);
  const pathParts = pathname.split('/').filter(Boolean);
  if (patternParts.length !== pathParts.length) return false;
  return patternParts.every((part, i) => part.startsWith(':') || part === pathParts[i]);
};

const MainLayout = () => {
  const user = useAuthStore((state) => state.user);
  const { title, subtitle } = usePageMeta();

  const initials = user ? `${user.firstName?.[0] ?? ''}`.toUpperCase() : '?';

  return (
    <div className={styles.layout}>
      <Sidebar />

      <main className={styles.content}>
        <header className={styles.topbar}>
          <div>
            <h1 className={styles.pageTitle}>{title}</h1>
            {subtitle && <p className={styles.pageSubtitle}>{subtitle}</p>}
          </div>

          <div className={styles.topbarActions}>
            <label className={styles.searchBox}>
              <LuSearch size={16} />
              <input type="text" placeholder="Ara..." aria-label="Ara" />
            </label>

            <button className={styles.iconButton} aria-label="Bildirimler">
              <LuBell size={19} />
              <span className={styles.notificationDot} />
            </button>
            {user && (
              <div className={styles.userChip}>
                <span className={styles.avatar}>{initials}</span>
                <span className={styles.userName}>{user.firstName}</span>
              </div>
            )}
          </div>
        </header>

        <div className={styles.scrollArea}>
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default MainLayout;
