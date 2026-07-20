import type { FC } from 'react';
import { LuCheck, LuX, LuInfo } from 'react-icons/lu';
import type { ToastItem } from './ToastContext';
import styles from './Toast.module.css';

interface ToastContainerProps {
  toasts: ToastItem[];
  onDismiss: (id: number) => void;
}

const ICONS = {
  success: <LuCheck size={16} />,
  error: <LuX size={16} />,
  info: <LuInfo size={16} />,
};

const ToastContainer: FC<ToastContainerProps> = ({ toasts, onDismiss }) => {
  if (toasts.length === 0) return null;

  return (
    <div className={styles.container}>
      {toasts.map((toast) => (
        <div key={toast.id} className={`${styles.toast} ${styles[toast.type]}`}>
          <span className={styles.icon}>{ICONS[toast.type]}</span>
          <span className={styles.message}>{toast.message}</span>
          <button
            type="button"
            className={styles.dismiss}
            onClick={() => onDismiss(toast.id)}
            aria-label="Bildirimi kapat"
          >
            <LuX size={14} />
          </button>
        </div>
      ))}
    </div>
  );
};

export default ToastContainer;