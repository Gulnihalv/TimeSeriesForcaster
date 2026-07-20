import { useState, type FC } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import Button from '../../../components/Button/Button';
import Spinner from '../../../components/Spinner/Spinner';
import { useApiData } from '../../../hooks/useApiData';
import { getModelComponents, type ModelComponents } from '../api/modelApi';
import styles from './ModelComponentsPanel.module.css';

interface ModelComponentsPanelProps {
  modelId: number;
}

const DAY_NAMES = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

const formatTrendDate = (iso: string) => new Date(iso).toLocaleDateString();

const ModelComponentsPanel: FC<ModelComponentsPanelProps> = ({ modelId }) => {
  const [isOpen, setIsOpen] = useState(false);

  // isOpen false iken gerçek istek atmıyoruz - kullanıcı "göster"e basmadan
  // gereksiz bir Prophet hesaplaması tetiklenmesin diye.
  const { data: components, isLoading, error } = useApiData<ModelComponents | null>(
    () => (isOpen ? getModelComponents(modelId) : Promise.resolve(null)),
    [modelId, isOpen],
    { fallbackErrorMessage: 'Model bileşenleri yüklenemedi.' }
  );

  if (!isOpen) {
    return (
      <div className={styles.toggleWrapper}>
        <Button variant="ghost" style={{ width: 'auto' }} onClick={() => setIsOpen(true)}>
          Model bileşenlerini göster
        </Button>
      </div>
    );
  }

  const trendData = components?.trend.map((p) => ({ label: formatTrendDate(p.label), value: p.value })) ?? [];
  const weeklyData =
    components?.weekly?.map((p) => ({
      label: DAY_NAMES[Number(p.label)] ?? p.label,
      value: p.value,
    })) ?? null;
  const yearlyData = components?.yearly?.map((p) => ({ label: `Gün ${p.label}`, value: p.value })) ?? null;

  return (
    <div className={styles.wrapper}>
      <div className={styles.header}>
        <h4 className={styles.title}>Model bileşenleri</h4>
        <Button variant="ghost" style={{ width: 'auto' }} onClick={() => setIsOpen(false)}>
          Gizle
        </Button>
      </div>

      {isLoading && <Spinner label="Bileşenler hesaplanıyor..." />}
      {error && <div className={styles.error}>{error}</div>}

      {components && (
        <div className={styles.grid}>
          <div className={styles.chartBlock}>
            <p className={styles.chartLabel}>Trend</p>
            <ResponsiveContainer width="100%" height={180}>
              <LineChart data={trendData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" tick={{ fontSize: 11 }} minTickGap={24} />
                <YAxis tick={{ fontSize: 11 }} domain={['auto', 'auto']} />
                <Tooltip />
                <Line type="monotone" dataKey="value" stroke="var(--color-primary)" strokeWidth={2} dot={false} isAnimationActive={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          {weeklyData && (
            <div className={styles.chartBlock}>
              <p className={styles.chartLabel}>Haftalık pattern</p>
              <ResponsiveContainer width="100%" height={180}>
                <LineChart data={weeklyData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} domain={['auto', 'auto']} />
                  <Tooltip />
                  <Line type="monotone" dataKey="value" stroke="var(--color-tone-green-text)" strokeWidth={2} isAnimationActive={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}

          {yearlyData && (
            <div className={styles.chartBlock}>
              <p className={styles.chartLabel}>Yıllık pattern</p>
              <ResponsiveContainer width="100%" height={180}>
                <LineChart data={yearlyData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} minTickGap={24} />
                  <YAxis tick={{ fontSize: 11 }} domain={['auto', 'auto']} />
                  <Tooltip />
                  <Line type="monotone" dataKey="value" stroke="var(--color-tone-amber-text)" strokeWidth={2} dot={false} isAnimationActive={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}

          {!weeklyData && !yearlyData && (
            <p className={styles.info}>
              Bu model için haftalık/yıllık mevsimsellik bileşeni tespit edilmedi (muhtemelen veri aralığı çok kısa).
            </p>
          )}
        </div>
      )}
    </div>
  );
};

export default ModelComponentsPanel;