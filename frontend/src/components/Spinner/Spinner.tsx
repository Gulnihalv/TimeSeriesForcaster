import type { FC } from 'react';
import styles from './Spinner.module.css';

interface SpinnerProps {
  label?: string;
  size?: number;
}

const Spinner: FC<SpinnerProps> = ({ label, size = 20 }) => {
  return (
    <div className={styles.wrapper}>
      <span className={styles.spinner} style={{ width: size, height: size }} />
      {label && <span className={styles.label}>{label}</span>}
    </div>
  );
};

export default Spinner;