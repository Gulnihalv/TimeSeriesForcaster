import { useContext } from 'react';
import { ToastContext, type ToastContextValue } from './toastState';

export const useToast = (): ToastContextValue => {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast, ToastProvider içinde kullanılmalı.');
  }
  return context;
};
