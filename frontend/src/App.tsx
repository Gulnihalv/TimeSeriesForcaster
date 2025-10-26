import { Routes, Route } from 'react-router-dom';
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import ProtectedRoute from './router/ProtectedRoute';
import GuestRoute from './router/GuestRoute';

import './App.css';

function App() {
  return (
    <Routes>
      {/* Misafir */}
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
        {/* register da buraya gelicek*/}
      </Route>

      {/* login olan kullanıcılar */}
      <Route element={<ProtectedRoute />}>
        <Route path="/dashboard" element={<DashboardPage />} />
        {/* login olanların routeları gelcek*/}
      </Route>

      {/* herkes */}
      <Route path="/" element={<HomePage />} />

      {/* hata vs. 404 gibi*/}
    </Routes>
  );
}

export default App;