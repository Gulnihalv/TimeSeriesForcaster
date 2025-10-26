import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const ProjectRoute = () => {
    const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

    if (!isAuthenticated){
        return <Navigate to="/login" replace />;
    }

    return <Outlet />;
}

export default ProjectRoute;