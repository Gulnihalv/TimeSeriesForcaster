import type { FC } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { useApiData } from '../../../hooks/useApiData';
import { getDataPointsForDataset, type DataPoint } from '../api/datasetApi';
import styles from './DatasetChart.module.css';

interface DatasetChartProps {
  datasetId: number;
}

interface ChartPoint {
  date: string;
  value: number;
  isOutlier: boolean;
}

interface OutlierDotProps {
  cx?: number;
  cy?: number;
  payload?: ChartPoint;
}

const formatDate = (iso: string) => new Date(iso).toLocaleDateString();

// Şu an backend'de gerçek bir outlier tespiti yapılmıyor (IsOutlier her zaman false),
// ama ileride eklenince bu nokta otomatik olarak kırmızı işaretlenecek - ekstra bir
// frontend değişikliği gerekmeden.
const OutlierDot = ({ cx, cy, payload }: OutlierDotProps) => {
  if (!payload?.isOutlier || cx === undefined || cy === undefined) return null;
  return <circle cx={cx} cy={cy} r={4} fill="var(--color-danger-text)" stroke="#fff" strokeWidth={1} />;
};

const DatasetChart: FC<DatasetChartProps> = ({ datasetId }) => {
  const { data: dataPoints, isLoading, error } = useApiData<DataPoint[]>(
    () => getDataPointsForDataset(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'Veri noktaları yüklenemedi.' }
  );

  if (isLoading) return <p className={styles.empty}>Grafik yükleniyor...</p>;
  if (error) return <p className={styles.empty}>{error}</p>;
  if (!dataPoints || dataPoints.length === 0) {
    return <p className={styles.empty}>Görüntülenecek veri yok.</p>;
  }

  const sorted = [...dataPoints].sort(
    (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
  );

  const chartData: ChartPoint[] = sorted.map((dp) => ({
    date: formatDate(dp.timestamp),
    value: dp.value,
    isOutlier: dp.isOutlier,
  }));

  return (
    <div className={styles.wrapper}>
      <ResponsiveContainer width="100%" height={240}>
        <LineChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="date" tick={{ fontSize: 12 }} minTickGap={24} />
          <YAxis tick={{ fontSize: 12 }} domain={['auto', 'auto']} />
          <Tooltip />
          <Line
            type="monotone"
            dataKey="value"
            name="Değer"
            stroke="var(--color-primary)"
            strokeWidth={2}
            dot={<OutlierDot />}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
};

export default DatasetChart;
