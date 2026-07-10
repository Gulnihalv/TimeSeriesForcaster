import type { FC } from 'react';
import {
  ComposedChart,
  Area,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import type { Prediction } from '../api/modelApi';
import styles from './ForecastChart.module.css';

interface ForecastChartProps {
  predictions: Prediction[];
}

interface ChartPoint {
  date: string;
  predictedValue: number;
  actualValue: number | null;
  // recharts'ın "band" (aralık) çizmesi için tek bir Area'ya [alt, üst] dizisi veriliyor
  range: [number, number];
}

const formatDate = (iso: string) => new Date(iso).toLocaleDateString();

const ForecastChart: FC<ForecastChartProps> = ({ predictions }) => {
  if (!predictions || predictions.length === 0) {
    return <p className={styles.empty}>Henüz tahmin oluşturulmadı.</p>;
  }

  const sorted = [...predictions].sort(
    (a, b) => new Date(a.predictionDate).getTime() - new Date(b.predictionDate).getTime()
  );

  const chartData: ChartPoint[] = sorted.map((p) => ({
    date: formatDate(p.predictionDate),
    predictedValue: p.predictedValue,
    actualValue: p.actualValue,
    range: [p.confidenceLower, p.confidenceUpper],
  }));

  const hasActual = sorted.some((p) => p.actualValue !== null);

  return (
    <div className={styles.wrapper}>
      <ResponsiveContainer width="100%" height={260}>
        <ComposedChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="date" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }} domain={['auto', 'auto']} />
          <Tooltip />
          <Legend />
          <Area
            type="monotone"
            dataKey="range"
            name="Güven aralığı"
            stroke="none"
            fill="var(--color-primary)"
            fillOpacity={0.15}
            isAnimationActive={false}
          />
          <Line
            type="monotone"
            dataKey="predictedValue"
            name="Tahmin"
            stroke="var(--color-primary)"
            strokeWidth={2}
            dot={false}
            isAnimationActive={false}
          />
          {hasActual && (
            <Line
              type="monotone"
              dataKey="actualValue"
              name="Gerçek"
              stroke="var(--color-status-completed-text)"
              strokeWidth={2}
              strokeDasharray="4 3"
              dot={false}
              connectNulls
              isAnimationActive={false}
            />
          )}
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
};

export default ForecastChart;