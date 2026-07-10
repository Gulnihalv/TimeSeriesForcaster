import React from 'react';
import Modal from '../Modal/Modal';
import Button from '../Button/Button';
import styles from './ConfirmDialog.module.css';

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  isLoading?: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Silme gibi geri alınamaz işlemler için genel amaçlı onay diyaloğu. */
const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  title,
  message,
  confirmLabel = 'Sil',
  isLoading = false,
  error,
  onConfirm,
  onCancel,
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onCancel} title={title}>
      <p className={styles.message}>{message}</p>
      {error && <div className={styles.error}>{error}</div>}
      <div className={styles.actions}>
        <Button variant="ghost" onClick={onCancel} disabled={isLoading} style={{ width: 'auto' }}>
          Vazgeç
        </Button>
        <Button variant="danger" onClick={onConfirm} disabled={isLoading} style={{ width: 'auto' }}>
          {isLoading ? 'Siliniyor...' : confirmLabel}
        </Button>
      </div>
    </Modal>
  );
};

export default ConfirmDialog;