import { useState, type FC, type FormEvent } from 'react';
import Button from '../../../components/Button/Button';
import { getErrorMessage } from '../../../api/errorUtils';
import { trainModel, type ProphetHyperparameters } from '../api/modelApi';
import { useToast } from '../../../components/Toast/ToastContext';
import { TOAST_MESSAGES } from '../../../constants/messages';
import styles from './ModelTrainingForm.module.css';

const ALGORITHMS = [
  { value: 'prophet', label: 'Prophet' },
];

interface ModelTrainingFormProps {
  datasetId: number;
  disabled?: boolean;
  disabledReason?: string;
  onModelCreated: () => void;
}

const ModelTrainingForm: FC<ModelTrainingFormProps> = ({
  datasetId,
  disabled = false,
  disabledReason,
  onModelCreated,
}) => {
  const [algorithm, setAlgorithm] = useState(ALGORITHMS[0].value);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { showToast } = useToast();

  const [seasonalityMode, setSeasonalityMode] = useState<'' | 'additive' | 'multiplicative'>('');
  const [changepointPriorScale, setChangepointPriorScale] = useState('');
  const [seasonalityPriorScale, setSeasonalityPriorScale] = useState('');
  const [changepointRange, setChangepointRange] = useState('');

  const buildHyperparameters = (): ProphetHyperparameters | undefined => {
    const hyperparameters: ProphetHyperparameters = {};
    if (seasonalityMode) hyperparameters.seasonalityMode = seasonalityMode;
    if (changepointPriorScale) hyperparameters.changepointPriorScale = Number(changepointPriorScale);
    if (seasonalityPriorScale) hyperparameters.seasonalityPriorScale = Number(seasonalityPriorScale);
    if (changepointRange) hyperparameters.changepointRange = Number(changepointRange);

    return Object.keys(hyperparameters).length > 0 ? hyperparameters : undefined;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      await trainModel(datasetId, algorithm, buildHyperparameters());
      onModelCreated();
      showToast(TOAST_MESSAGES.modelTrainingStarted, 'success');
    } catch (err) {
      setError(getErrorMessage(err, 'Model eğitimi başlatılamadı.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      {error && <div className={styles.error}>{error}</div>}
      {disabled && disabledReason && (
        <div className={styles.hint}>{disabledReason}</div>
      )}

      <div className={styles.row}>
        <select
          className={styles.select}
          value={algorithm}
          onChange={(e) => setAlgorithm(e.target.value)}
          disabled={disabled || isSubmitting}
        >
          {ALGORITHMS.map((a) => (
            <option key={a.value} value={a.value}>{a.label}</option>
          ))}
        </select>
        <Button type="submit" disabled={disabled || isSubmitting} style={{ width: 'auto' }}>
          {isSubmitting ? 'Başlatılıyor...' : 'Model Eğit'}
        </Button>
      </div>

      <details className={styles.advanced}>
        <summary className={styles.advancedSummary}>Gelişmiş ayarlar</summary>

        <div className={styles.advancedGrid}>
          <label className={styles.fieldLabel}>
            Mevsimsellik modu
            <select
              className={styles.select}
              value={seasonalityMode}
              onChange={(e) => setSeasonalityMode(e.target.value as '' | 'additive' | 'multiplicative')}
              disabled={disabled || isSubmitting}
            >
              <option value="">Varsayılan</option>
              <option value="additive">Additive</option>
              <option value="multiplicative">Multiplicative</option>
            </select>
          </label>

          <label className={styles.fieldLabel}>
            Changepoint prior scale
            <input
              type="number"
              step="0.01"
              min="0"
              className={styles.numberInput}
              placeholder="0.05 (varsayılan)"
              value={changepointPriorScale}
              onChange={(e) => setChangepointPriorScale(e.target.value)}
              disabled={disabled || isSubmitting}
            />
          </label>

          <label className={styles.fieldLabel}>
            Seasonality prior scale
            <input
              type="number"
              step="0.1"
              min="0"
              className={styles.numberInput}
              placeholder="10 (varsayılan)"
              value={seasonalityPriorScale}
              onChange={(e) => setSeasonalityPriorScale(e.target.value)}
              disabled={disabled || isSubmitting}
            />
          </label>

          <label className={styles.fieldLabel}>
            Changepoint range
            <input
              type="number"
              step="0.05"
              min="0"
              max="1"
              className={styles.numberInput}
              placeholder="0.8 (varsayılan)"
              value={changepointRange}
              onChange={(e) => setChangepointRange(e.target.value)}
              disabled={disabled || isSubmitting}
            />
          </label>
        </div>
      </details>
    </form>
  );
};

export default ModelTrainingForm;