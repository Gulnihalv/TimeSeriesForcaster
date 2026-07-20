import Card from '../components/Card/Card';
import { useAuthStore } from '../store/authStore';
import { LuUserRound, LuBellRing, LuPalette, LuShieldCheck } from 'react-icons/lu';
import styles from './SettingsPage.module.css';

const UPCOMING_SECTIONS = [
  {
    icon: LuBellRing,
    tone: 'blue' as const,
    title: 'Bildirimler',
    description: 'Model eğitimi bittiğinde veya bir dataset hata verdiğinde e-posta/push bildirimi al.',
  },
  {
    icon: LuPalette,
    tone: 'violet' as const,
    title: 'Görünüm',
    description: 'Koyu tema ve yoğunluk (kompakt/ferah) seçenekleri yakında burada olacak.',
  },
  {
    icon: LuShieldCheck,
    tone: 'green' as const,
    title: 'Güvenlik',
    description: 'Şifre değiştirme, iki adımlı doğrulama ve aktif oturumların listesi.',
  },
];

const SettingsPage = () => {
  const user = useAuthStore((state) => state.user);
  const fullName = user ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() : '';
  const initials = user ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase() : '?';

  return (
    <div className={styles.grid}>
      <Card className={styles.profileCard}>
        <div className={styles.profileHeader}>
          <span className={styles.avatar}>{initials}</span>
          <div>
            <h2 className={styles.name}>{fullName || 'Kullanıcı'}</h2>
            <p className={styles.email}>{user?.email || '—'}</p>
          </div>
        </div>
        <p className={styles.profileHint}>
          Profil bilgilerini düzenleme özelliği yakında eklenecek. Şimdilik hesabın bu bilgilerle kayıtlı.
        </p>
      </Card>

      {/* <div className={styles.sectionList}>
        {UPCOMING_SECTIONS.map(({ icon: Icon, tone, title, description }) => (
          <Card key={title} className={styles.sectionCard}>
            <div className={`${styles.sectionIcon} ${styles[`tone_${tone}`]}`}>
              <Icon size={20} />
            </div>
            <div className={styles.sectionText}>
              <div className={styles.sectionTitleRow}>
                <h3 className={styles.sectionTitle}>{title}</h3>
                <span className={styles.soonBadge}>Yakında</span>
              </div>
              <p className={styles.sectionDescription}>{description}</p>
            </div>
          </Card>
        ))}
      </div> */}
    </div>
  );
};

export default SettingsPage;
