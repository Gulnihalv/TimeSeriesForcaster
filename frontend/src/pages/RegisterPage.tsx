import { LuChartLine } from 'react-icons/lu';
import { Link } from 'react-router-dom';
import RegisterForm from '../features/authentication/components/RegisterForm';
import styles from './LoginPage.module.css';

const RegisterPage = () => {
  return (
    <div className={styles.container}>
      <div className={styles.stack}>
        <div className={styles.brand}>
          <span className={styles.brandLogo}>
            <LuChartLine size={20} />
          </span>
          <span className={styles.brandName}>Tahmin Platformu</span>
        </div>
        <RegisterForm />
        <p style={{ textAlign: 'center', marginTop: 16, fontSize: '0.875rem', color: 'var(--color-text-secondary)' }}>
          Zaten hesabın var mı? <Link to="/login">Giriş yap</Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;