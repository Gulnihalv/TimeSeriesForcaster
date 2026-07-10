import { LuChartLine } from 'react-icons/lu';
import LoginForm from '../features/authentication/components/LoginForm';
import styles from './LoginPage.module.css';

const LoginPage = () => {
  return (
    <div className={styles.container}>
      <div className={styles.stack}>
        <div className={styles.brand}>
          <span className={styles.brandLogo}>
            <LuChartLine size={20} />
          </span>
          <span className={styles.brandName}>Tahmin Platformu</span>
        </div>
        <LoginForm />
      </div>
    </div>
  );
};

export default LoginPage;