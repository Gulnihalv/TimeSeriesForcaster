import { useState, type FC, type FormEvent } from 'react';
import Button from '../../../components/Button/Button';
import { getErrorMessage } from '../../../api/errorUtils';
import { trainModel } from '../api/modelApi';
import styles from './ModelTrainingForm.module.css';

const ALGORITHMS = [
  { value: 'prophet', label: 'Prophet' },
];

interface ModelTrainingFormProps {
  datasetId: number;
  /** Dataset henüz işlenmediyse veya hata aldıysa formu devre dışı bırak */
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

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      await trainModel(datasetId, algorithm);
      onModelCreated();
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
    </form>
  );
};

export default ModelTrainingForm;
