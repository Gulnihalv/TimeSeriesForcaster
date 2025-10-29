import Card from '../components/Card/Card';
import styles from './DashboardPage.module.css';

const DashboardPage = () => {


  return (
    <div className={styles.dashboardGrid}>
      <Card>
        <div className={styles.welcomeCard}>
          <div>
            <h2>Hello World</h2>
            <p>
              Uploading yoğun proje android de bir şey yok ki bu kadar
              çok bir de bir tane var bir de bu
            </p>
            <button className={styles.welcomeButton}>Create Project</button>
          </div>
          <div>
            {/* Resim */}
            <img 
              src="https://placehold.co/300x200" 
              alt="Dashboard illustration" 
              className={styles.welcomeImage}
            />
          </div>
        </div>
      </Card>

      {/* Diğer kartlar (Notifications vb.) buraya gelecek */}
    </div>
  );
};

export default DashboardPage;