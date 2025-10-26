import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Card from '../../../components/Card/Card';
import Input from '../../../components/Input/Input';
import Button from '../../../components/Button/Button';
import { login } from '../api/authApi';
import styles from './LoginForm.module.css'; 

const LoginForm = () => {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      await login({ email, password });
      setIsLoading(false);
      navigate('/dashboard'); 

    } catch (err) {
      setIsLoading(false);
      setError('Kullanıcı adı veya şifre hatalı.');
      console.error('Login hatası:', err);
    }
  };


  return (
    <Card>
      <form onSubmit={handleSubmit}>
        <h2>Giriş Yap</h2>

        {error && <div className={styles.error}>{error}</div>}

        <div className={styles.formGroup}>
          <label htmlFor="email">Email</label>
          <Input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="password">Şifre</label>
          <Input
            type="password"
            id="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        <Button type="submit" disabled={isLoading}>
          {isLoading ? 'Giriş Yapılıyor...' : 'Giriş Yap'}
        </Button>
      </form>
    </Card>
  );
};

export default LoginForm;