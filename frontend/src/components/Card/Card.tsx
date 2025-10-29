import React from 'react';
import styles from './Card.module.css';

type CardProps = React.HTMLAttributes<HTMLDivElement>;

const Card: React.FC<CardProps> = ({ 
  children, 
  className,
  ...rest
}) => {
  
  const combinedClassName = `${styles.card} ${className || ''}`;

  return (
    <div className={combinedClassName} {...rest}>
      {children}
    </div>
  );
};

export default Card;