import { useEffect, useRef } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { LuChartLine, LuLogOut } from 'react-icons/lu';
import { RiHomeLine, RiSettings4Line } from 'react-icons/ri';
import { BsGrid } from 'react-icons/bs';
import styles from './Sidebar.module.css';

const NAV_ITEMS = [
    { to: '/dashboard', label: 'Ana Sayfa', icon: RiHomeLine },
    { to: '/projects', label: 'Projeler', icon: BsGrid },
    { to: '/settings', label: 'Ayarlar', icon: RiSettings4Line },
];

const SideBar = () => {
    const logout = useAuthStore((state) => state.logout);
    const navigate = useNavigate();

    const indicatorRef = useRef<HTMLDivElement>(null);
    const navListRef = useRef<HTMLUListElement>(null);
    const location = useLocation();

    useEffect(() => {
        const navList = navListRef.current;
        const indicator = indicatorRef.current;
        if (!navList || !indicator) return;
        const activeLink = navList.querySelector<HTMLAnchorElement>('a[aria-current="page"]');

        if (activeLink) {
            const activeLi = activeLink.parentElement as HTMLLIElement;
            indicator.style.top = `${activeLi.offsetTop}px`;
            indicator.style.height = `${activeLi.offsetHeight}px`;
            indicator.style.opacity = '1';
        } else {
            indicator.style.opacity = '0';
        }
    }, [location]);

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    return (
        <aside className={styles.sidebar}>
            <div className={styles.sidebarTop}>
                <div className={styles.logo}>
                    <LuChartLine size={22} />
                </div>
                <nav>
                    <ul ref={navListRef} className={styles.navList}>
                        <div ref={indicatorRef} className={styles.indicator} />
                        {NAV_ITEMS.map(({ to, label, icon: Icon }) => (
                            <li key={to} className={styles.navItem} data-tooltip={label}>
                                <NavLink
                                    to={to}
                                    className={({ isActive }) =>
                                        `${styles.navLink} ${isActive ? styles.active : ''}`
                                    }
                                >
                                    <Icon size={20} />
                                </NavLink>
                            </li>
                        ))}
                    </ul>
                </nav>
            </div>
            <div className={styles.sidebarBottom}>
                <button
                    onClick={handleLogout}
                    className={styles.logoutButton}
                    data-tooltip="Çıkış yap"
                    aria-label="Çıkış yap"
                >
                    <LuLogOut size={20} />
                </button>
            </div>
        </aside>
    );
};

export default SideBar;
