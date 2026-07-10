import React from 'react';
import styles from './Card.module.css';

type CardProps = React.HTMLAttributes<HTMLDivElement> & {
  /** Tıklanabilir liste kartları için hover-lift efekti (ProjectList, DatasetList, ModelList vb.) */
  interactive?: boolean;
};

const Card: React.FC<CardProps> = ({ 
  children, 
  className,
  interactive = false,
  ...rest
}) => {
  
  const combinedClassName = `${styles.card} ${interactive ? styles.interactive : ''} ${className || ''}`;

  return (
    <div className={combinedClassName} {...rest}>
      {children}
    </div>
  );
};

export default Card;