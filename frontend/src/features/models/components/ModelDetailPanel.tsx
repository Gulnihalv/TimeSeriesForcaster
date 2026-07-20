import { useCallback, useEffect, useRef, useState, type FC, type FormEvent } from 'react';
import Card from '../../../components/Card/Card';
import { StatusBadge } from '../../../components/StatusBadge/StatusBadge';
import Button from '../../../components/Button/Button';
import Input from '../../../components/Input/Input';
import { useApiData } from '../../../hooks/useApiData';
import { getErrorMessage } from '../../../api/errorUtils';
import {
  getModelById,
  generateForecast,
  ModelStatus,
  MetricName,
  type ModelDetail,
} from '../api/modelApi';
import ForecastChart from './ForecastChart';
import ModelComponentsPanel from './ModelComponentsPanel';
import styles from './ModelDetailPanel.module.css';

interface ModelDetailPanelProps {
  modelId: number;
}

const METRIC_LABELS: Record<MetricName, string> = {
  [MetricName.MAE]: 'MAE',
  [MetricName.RMSE]: 'RMSE',
};

const METRIC_TONES = ['violet', 'blue', 'amber', 'green'] as const;

const ModelDetailPanel: FC<ModelDetailPanelProps> = ({ modelId }) => {
  const [horizon, setHorizon] = useState(30);
  const [isTriggering, setIsTriggering] = useState(false);
  const [triggerError, setTriggerError] = useState<string | null>(null);
  const [awaitingForecast, setAwaitingForecast] = useState(false);
  const predictionCountBeforeTrigger = useRef(0);

  const shouldPoll = useCallback(
    (model: ModelDetail) => model.status === ModelStatus.Training || awaitingForecast,
    [awaitingForecast]
  );

  const { data: model, isLoading, error } = useApiData<ModelDetail>(
    () => getModelById(modelId),
    [modelId],
    { fallbackErrorMessage: 'Model detayı yüklenemedi.', shouldPoll, pollIntervalMs: 3000 }
  );

  useEffect(() => {
    if (awaitingForecast && model && model.predictions.length > predictionCountBeforeTrigger.current) {
      setAwaitingForecast(false);
    }
  }, [model, awaitingForecast]);

  const handleGenerateForecast = async (e: FormEvent) => {
    e.preventDefault();
    if (!model) return;
    setIsTriggering(true);
    setTriggerError(null);
    predictionCountBeforeTrigger.current = model.predictions.length;
    try {
      await generateForecast(modelId, horizon);
      setAwaitingForecast(true);
    } catch (err) {
      setTriggerError(getErrorMessage(err, 'Tahmin oluşturulamadı.'));
    } finally {
      setIsTriggering(false);
    }
  };

  if (isLoading) return <Card>Model detayı yükleniyor...</Card>;
  if (error || !model) return <div className={styles.error}>{error || 'Model bulunamadı.'}</div>;

  return (
    <Card>
      <div className={styles.header}>
        <div>
          <h2 className={styles.title}>{model.modelName || model.algorithm}</h2>
          <p className={styles.subtitle}>{model.algorithm}</p>
        </div>
        <StatusBadge status={model.status} />
      </div>

      {model.status === ModelStatus.Failed && model.errorMessage && (
        <div className={styles.error}>{model.errorMessage}</div>
      )}

      {model.metrics.length > 0 && (
        <div className={styles.metricGrid}>
          {model.metrics.map((metric, index) => (
            <div
              key={metric.metricName}
              className={`${styles.metricCard} ${styles[`tone_${METRIC_TONES[index % METRIC_TONES.length]}`]}`}
            >
              <span className={styles.metricLabel}>{METRIC_LABELS[metric.metricName]}</span>
              <span className={styles.metricValue}>{metric.metricValue.toFixed(2)}</span>
            </div>
          ))}
        </div>
      )}

      <div className={styles.chartSection}>
        <ForecastChart predictions={model.predictions} />
      </div>

      {model.status === ModelStatus.Completed && (
        <form onSubmit={handleGenerateForecast} className={styles.forecastForm}>
          {triggerError && <div className={styles.error}>{triggerError}</div>}
          <label className={styles.horizonLabel} htmlFor="horizon">
            Tahmin ufku (gün)
          </label>
          <div className={styles.row}>
            <Input
              id="horizon"
              type="number"
              min={1}
              max={365}
              value={horizon}
              onChange={(e) => setHorizon(Number(e.target.value))}
              disabled={isTriggering || awaitingForecast}
            />
            <Button type="submit" disabled={isTriggering || awaitingForecast} style={{ width: 'auto' }}>
              {awaitingForecast ? 'Oluşturuluyor...' : 'Tahmin Oluştur'}
            </Button>
          </div>
        </form>
      )}

      {model.status === ModelStatus.Completed && (
        <ModelComponentsPanel modelId={modelId} />
      )}
    </Card>
  );
};

export default ModelDetailPanel;