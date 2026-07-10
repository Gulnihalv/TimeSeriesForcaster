import React from 'react';
import styles from './Card.module.css';

type CardTone = 'default' | 'violet' | 'green' | 'amber' | 'blue' | 'rose' | 'dark';

type CardProps = React.HTMLAttributes<HTMLDivElement> & {
  interactive?: boolean;
  tone?: CardTone;
};

const TONE_CLASS: Record<CardTone, string> = {
  default: '',
  violet: 'toneViolet',
  green: 'toneGreen',
  amber: 'toneAmber',
  blue: 'toneBlue',
  rose: 'toneRose',
  dark: 'toneDark',
};

const Card: React.FC<CardProps> = ({
  children,
  className,
  interactive = false,
  tone = 'default',
  ...rest
}) => {

  const toneClass = TONE_CLASS[tone] ? styles[TONE_CLASS[tone]] : '';
  const combinedClassName = `${styles.card} ${interactive ? styles.interactive : ''} ${toneClass} ${className || ''}`;

  return (
    <div className={combinedClassName} {...rest}>
      {children}
    </div>
  );
};

export default Card;
