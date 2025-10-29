import { useEffect, useRef } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { LuChartLine, LuLogOut } from 'react-icons/lu';
import { RiHomeLine, RiSettings4Line } from 'react-icons/ri';
import styles from './Sidebar.module.css';
import { BsGrid } from 'react-icons/bs';

const SideBar = () => {
    const logout = useAuthStore((state) => state.logout);
    const navigate = useNavigate();

    const selectorRef = useRef<HTMLDivElement>(null);
    const navListRef = useRef<HTMLUListElement>(null);
    const location = useLocation();

    useEffect(() => {
        const navList = navListRef.current;
        const selector = selectorRef.current;
        if (!navList || !selector) return;
        const activeLink = navList.querySelector<HTMLAnchorElement>('a[aria-current="page"]');

        if (activeLink) {
            const activeLi = activeLink.parentElement as HTMLLIElement;
            const top = activeLi.offsetTop;
            const height = activeLi.offsetHeight;

            selector.style.top = `${top}px`;
            selector.style.height = `${height}px`;
            selector.style.opacity = '1';
        } else {
            selector.style.opacity = '0';
        }

    }, [location]);

    const handleLogout = () => {
        logout();
        navigate('/login');
    }

    return (
        <aside className={styles.sidebar}>
            <div className={styles.sidebarTop}>
                <div className={styles.logo}>
                    {/* logo kısmı ama bu değişebilir */}
                    <LuChartLine size={30} />
                </div>
                <nav>
                    <ul ref={navListRef} className={styles.navList}>
                        <div ref={selectorRef} className={styles.horiSelector}>
                            <div className={styles.topCurve}></div>
                            <div className={styles.bottomCurve}></div>
                        </div>
                        <li>
                            <NavLink 
                                to="/dashboard"
                                className={({isActive}) => 
                                    `${styles.navLink} ${isActive ? styles.active : ""}`
                                }
                                >
                                <RiHomeLine size={22} />
                            </NavLink>
                        </li>
                        <li>
                            <NavLink 
                                to="/projects"
                                className={({isActive}) => 
                                    `${styles.navLink} ${isActive ? styles.active : ""}`
                                }
                                >
                                <BsGrid size={22} />
                            </NavLink>
                        </li>
                        <li>
                            <NavLink 
                                to="/settings"
                                className={({isActive}) => 
                                    `${styles.navLink} ${isActive ? styles.active : ""}`
                                }
                                >
                                <RiSettings4Line size={22} />
                            </NavLink>
                        </li>
                    </ul>
                </nav>
            </div>
            <div className={styles.sidebarBottom}>
                <button onClick={handleLogout} className={styles.logoutButton}>
                    <LuLogOut size={22} />
                </button>
            </div>
        </aside>
    );

}

export default SideBar;