import React from 'react';
import styles from './Button.module.css';

type ButtonVariant = 'primary' | 'dark' | 'ghost' | 'white' | 'danger';

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
};

const VARIANT_CLASS: Record<ButtonVariant, string> = {
  primary: '',
  dark: 'dark',
  ghost: 'ghost',
  white: 'white',
  danger: 'danger',
};

const Button: React.FC<ButtonProps> = ({ children, className, variant = 'primary', ...props }) => {
  const variantClass = VARIANT_CLASS[variant] ? styles[VARIANT_CLASS[variant]] : '';
  return (
    <button className={`${styles.button} ${variantClass} ${className || ''}`} {...props}>
      {children}
    </button>
  );
};

export default Button;