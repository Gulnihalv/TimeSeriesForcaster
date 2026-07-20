import { Routes, Route } from 'react-router-dom';

import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import ProjectsPage from './pages/ProjectsPage';
import ProjectDetailPage from './pages/ProjectDetailPage';
import SettingsPage from './pages/SettingsPage';
import ProtectedRoute from './router/ProtectedRoute';
import GuestRoute from './router/GuestRoute';
import MainLayout from './components/Layout/MainLayout';
import DatasetDetailPage from './pages/DatasetDetailPage';
import { ToastProvider } from './components/Toast/ToastContext';


function App() {
  return (
    <ToastProvider>
      <Routes>
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/settings" element={<SettingsPage />} />

          <Route path="/projects/:projectId" element={<ProjectDetailPage />} />
          <Route path="/datasets/:datasetId" element={<DatasetDetailPage />} />
        </Route>
      </Route>

      <Route path="/" element={<HomePage />} />

    </Routes>
    </ToastProvider>
    
  );
}

export default App;