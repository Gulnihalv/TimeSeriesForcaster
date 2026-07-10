import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const HomePage = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  return <Navigate to={isAuthenticated ? '/dashboard' : '/login'} replace />;
};

export default HomePage;
