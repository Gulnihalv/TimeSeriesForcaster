import type { FC } from 'react';
import {
  ComposedChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { useApiData } from '../../../hooks/useApiData';
import { getModelById, MetricName, ModelStatus, type ModelDetail } from '../api/modelApi';
import { LuX } from 'react-icons/lu';
import styles from './ModelComparisonView.module.css';

interface ModelComparisonViewProps {
  modelIds: number[];
  onRemoveModel?: (modelId: number) => void;
}

const LINE_COLORS = ['#5B4FE0', '#1F9D66', '#C98A1B', '#C23164'];

interface ParsedHyperparameters {
  seasonalityMode?: string;
  changepointPriorScale?: number;
  seasonalityPriorScale?: number;
  changepointRange?: number;
}

const parseHyperparameters = (raw: string | null): ParsedHyperparameters => {
  if (!raw) return {};
  try {
    return JSON.parse(raw) as ParsedHyperparameters;
  } catch {
    return {};
  }
};

const HYPERPARAM_ROWS: { key: keyof ParsedHyperparameters; label: string }[] = [
  { key: 'seasonalityMode', label: 'Mevsimsellik modu' },
  { key: 'changepointPriorScale', label: 'Changepoint prior scale' },
  { key: 'seasonalityPriorScale', label: 'Seasonality prior scale' },
  { key: 'changepointRange', label: 'Changepoint range' },
];

const getMetricValue = (model: ModelDetail, metricName: MetricName): number | null => {
  const metric = model.metrics.find((m) => m.metricName === metricName);
  return metric ? metric.metricValue : null;
};

const formatDate = (iso: string) => new Date(iso).toLocaleDateString();

const ModelComparisonView: FC<ModelComparisonViewProps> = ({ modelIds, onRemoveModel }) => {
  const { data: models, isLoading, error } = useApiData<ModelDetail[]>(
    () => Promise.all(modelIds.map((id) => getModelById(id))),
    [modelIds.join(',')],
    {
      fallbackErrorMessage: 'Modeller yüklenemedi.',
      shouldPoll: (list) => list.some((m) => m.status === ModelStatus.Training || m.status === ModelStatus.Queued),
    }
  );

  if (isLoading) return <p className={styles.info}>Karşılaştırma yükleniyor...</p>;
  if (error) return <p className={styles.error}>{error}</p>;
  if (!models || models.length === 0) return null;

  const hyperparametersByModel = models.map((m) => parseHyperparameters(m.hyperparameters));

  // Metrik tablosu için satır başına en düşük (en iyi) değeri buluyoruz
  const maeValues = models.map((m) => getMetricValue(m, MetricName.MAE));
  const rmseValues = models.map((m) => getMetricValue(m, MetricName.RMSE));
  const validMae = maeValues.filter((v): v is number => v !== null);
  const validRmse = rmseValues.filter((v): v is number => v !== null);
  const bestMae = validMae.length > 0 ? Math.min(...validMae) : null;
  const bestRmse = validRmse.length > 0 ? Math.min(...validRmse) : null;

  // Forecast grafiği için: tüm modellerin tahmin tarihlerinin birleşimini alıp
  // her tarih için her modelin tahmin değerini (varsa) tek bir satıra topluyoruz.
  const dateSet = new Set<string>();
  models.forEach((m) => m.predictions.forEach((p) => dateSet.add(p.predictionDate)));
  const sortedDates = Array.from(dateSet).sort();

  const chartData = sortedDates.map((date) => {
    const point: Record<string, string | number | null> = { date: formatDate(date) };
    models.forEach((m) => {
      const pred = m.predictions.find((p) => p.predictionDate === date);
      point[`model_${m.id}`] = pred ? pred.predictedValue : null;
    });
    return point;
  });

  return (
    <div className={styles.wrapper}>
      <div className={styles.modelChips}>
        {models.map((m, index) => (
          <div key={m.id} className={styles.chip} style={{ borderColor: LINE_COLORS[index % LINE_COLORS.length] }}>
            <span className={styles.chipDot} style={{ backgroundColor: LINE_COLORS[index % LINE_COLORS.length] }} />
            <span>{m.modelName || m.algorithm}</span>
            {onRemoveModel && (
              <button
                type="button"
                className={styles.chipRemove}
                onClick={() => onRemoveModel(m.id)}
                aria-label={`${m.modelName} karşılaştırmadan çıkar`}
              >
                <LuX size={13} />
              </button>
            )}
          </div>
        ))}
      </div>

      <div className={styles.section}>
        <h4 className={styles.sectionTitle}>Metrikler</h4>
        <table className={styles.table}>
          <thead>
            <tr>
              <th></th>
              {models.map((m) => <th key={m.id}>{m.modelName || m.algorithm}</th>)}
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>MAE</td>
              {maeValues.map((v, i) => (
                <td key={models[i].id} className={bestMae !== null && v === bestMae ? styles.bestCell : ''}>
                  {v !== null ? v.toFixed(3) : '—'}
                </td>
              ))}
            </tr>
            <tr>
              <td>RMSE</td>
              {rmseValues.map((v, i) => (
                <td key={models[i].id} className={bestRmse !== null && v === bestRmse ? styles.bestCell : ''}>
                  {v !== null ? v.toFixed(3) : '—'}
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>

      <div className={styles.section}>
        <h4 className={styles.sectionTitle}>Hiperparametreler</h4>
        <table className={styles.table}>
          <thead>
            <tr>
              <th></th>
              {models.map((m) => <th key={m.id}>{m.modelName || m.algorithm}</th>)}
            </tr>
          </thead>
          <tbody>
            {HYPERPARAM_ROWS.map((row) => {
              const values = hyperparametersByModel.map((h) => h[row.key]);
              const allSame = values.every((v) => v === values[0]);
              return (
                <tr key={row.key}>
                  <td>{row.label}</td>
                  {values.map((v, i) => (
                    <td key={models[i].id} className={!allSame ? styles.diffCell : ''}>
                      {v !== undefined && v !== null && v !== '' ? String(v) : 'Varsayılan'}
                    </td>
                  ))}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className={styles.section}>
        <h4 className={styles.sectionTitle}>Tahmin grafiği (üst üste)</h4>
        {chartData.length === 0 ? (
          <p className={styles.info}>Henüz hiçbir modelde tahmin üretilmemiş.</p>
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <ComposedChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} domain={['auto', 'auto']} />
              <Tooltip />
              <Legend />
              {models.map((m, index) => (
                <Line
                  key={m.id}
                  type="monotone"
                  dataKey={`model_${m.id}`}
                  name={m.modelName || m.algorithm}
                  stroke={LINE_COLORS[index % LINE_COLORS.length]}
                  strokeWidth={2}
                  dot={false}
                  connectNulls
                  isAnimationActive={false}
                />
              ))}
            </ComposedChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  );
};

export default ModelComparisonView;