import { Routes, Route } from 'react-router-dom';

// Sayfalar
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import ProjectsPage from './pages/ProjectsPage'; // YENİ
import SettingsPage from './pages/SettingsPage'; // YENİ

// Router mantığı
import ProtectedRoute from './router/ProtectedRoute';
import GuestRoute from './router/GuestRoute';
import MainLayout from './components/Layout/MainLayout'; // YENİ

import './App.css';

function App() {
  return (
    <Routes>
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />

      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
      </Route>

      <Route path="/" element={<HomePage />} />

    </Routes>
  );
}

export default App;