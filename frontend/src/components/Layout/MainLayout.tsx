import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import styles from './MainLayout.module.css';

const MainLayout = () => {
  return (
    <div className={styles.layout}>
      <Sidebar />

      <main>
        {/* Sayfa içerikleri*/}
        <Outlet />
      </main>
    </div>
  );
};

export default MainLayout;