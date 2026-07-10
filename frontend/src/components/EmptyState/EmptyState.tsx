import React from 'react';
import styles from './EmptyState.module.css';

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
  tone?: 'muted' | 'card';
}

/**
 * Boş/işlenmemiş alanlar için tekrar kullanılabilir placeholder.
 * Kullanıcının "burada ne olacak, nereye tıklayacağım" sorusuna
 * her zaman bir ikon + açıklama + (varsa) aksiyon ile cevap verir.
 */
const EmptyState: React.FC<EmptyStateProps> = ({ icon, title, description, action, tone = 'card' }) => {
  return (
    <div className={`${styles.wrapper} ${tone === 'card' ? styles.card : styles.muted}`}>
      {icon && <div className={styles.icon}>{icon}</div>}
      <p className={styles.title}>{title}</p>
      {description && <p className={styles.description}>{description}</p>}
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
};

export default EmptyState;
