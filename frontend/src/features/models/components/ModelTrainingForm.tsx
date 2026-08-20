import { useEffect, useState, type FC, type FormEvent } from 'react';
import { LuChevronDown } from 'react-icons/lu';
import Button from '../../../components/Button/Button';
import Modal from '../../../components/Modal/Modal';
import { getErrorMessage } from '../../../api/errorUtils';
import {
  trainModel,
  getResolutionOptions,
  TimeResolution,
  AggregationFunction,
  RESOLUTION_LABELS,
  type ProphetHyperparameters,
  type ResolutionOption,
} from '../api/modelApi';
import { useApiData } from '../../../hooks/useApiData';
import { useToast } from '../../../components/Toast/ToastContext';
import { TOAST_MESSAGES } from '../../../constants/messages';
import styles from './ModelTrainingForm.module.css';

const ALGORITHMS = [
  { value: 'prophet', label: 'Prophet' },
];

const HYPERPARAM_COUNT = 4;

interface ModelTrainingFormProps {
  datasetId: number;
  disabled?: boolean;
  disabledReason?: string;
  onModelCreated: () => void;
}

type Scenario = 'loading' | 'fetchError' | 'disabled' | 'mandatory' | 'optional' | 'rawOnly';

const formatPoints = (n: number) => n.toLocaleString('tr-TR');

// Aynı nokta sayısına sahip seçenekleri eler: her grupta önce "önerilen" olan,
// yoksa en kaba (en büyük resolution değerine sahip) olan tutulur. Örn. veri zaten
// günlük frekanslıysa Dakikalık/Saatlik/Günlük hepsi Ham ile birebir aynı sayıyı
// verir ve bunları ayrı seçenekmiş gibi göstermenin bir anlamı yoktur.
const dedupeByPoints = (options: ResolutionOption[]): ResolutionOption[] => {
  const groups = new Map<number, ResolutionOption[]>();
  options.forEach((o) => groups.set(o.estimatedPoints, [...(groups.get(o.estimatedPoints) ?? []), o]));
  return options.filter((o) => {
    const group = groups.get(o.estimatedPoints)!;
    if (group.length === 1) return true;
    const chosen = group.find((g) => g.isRecommended) ?? group.reduce((a, b) => (a.resolution > b.resolution ? a : b));
    return o === chosen;
  });
};

const ModelTrainingForm: FC<ModelTrainingFormProps> = ({
  datasetId,
  disabled = false,
  disabledReason,
  onModelCreated,
}) => {
  const [algorithm, setAlgorithm] = useState(ALGORITHMS[0].value);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { showToast } = useToast();

  const [seasonalityMode, setSeasonalityMode] = useState<'' | 'additive' | 'multiplicative'>('');
  const [changepointPriorScale, setChangepointPriorScale] = useState('');
  const [seasonalityPriorScale, setSeasonalityPriorScale] = useState('');
  const [changepointRange, setChangepointRange] = useState('');

  const [selectedResolution, setSelectedResolution] = useState<TimeResolution | null>(null);
  const [selectedAggregation, setSelectedAggregation] = useState<AggregationFunction>(AggregationFunction.Average);

  const {
    data: resolutionOptions,
    isLoading: isLoadingResolutions,
    error: resolutionError,
  } = useApiData<ResolutionOption[]>(
    () => getResolutionOptions(datasetId),
    [datasetId],
    { fallbackErrorMessage: 'Çözünürlük seçenekleri yüklenemedi.' }
  );

  const allowedOptions = (resolutionOptions ?? []).filter((o) => o.isAllowed);
  const hasNoAllowedOptions = resolutionOptions !== null && allowedOptions.length === 0;
  const dedupedOptions = dedupeByPoints(allowedOptions);
  const rawOption = resolutionOptions?.find((o) => o.resolution === TimeResolution.Raw);
  const rawAllowed = rawOption?.isAllowed === true;

  const scenario: Scenario = isLoadingResolutions
    ? 'loading'
    : resolutionError
    ? 'fetchError'
    : hasNoAllowedOptions
    ? 'disabled'
    : !rawAllowed
    ? 'mandatory'
    : dedupedOptions.some((o) => o.resolution !== TimeResolution.Raw)
    ? 'optional'
    : 'rawOnly';

  const showResolutionPicker = scenario === 'mandatory' || scenario === 'optional';

  useEffect(() => {
    if (!showResolutionPicker || selectedResolution !== null) return;
    const recommended = dedupedOptions.find((o) => o.isRecommended) ?? dedupedOptions[0];
    if (recommended) setSelectedResolution(recommended.resolution);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [showResolutionPicker, dedupedOptions.map((o) => o.resolution).join(',')]);

  const selectedOption = dedupedOptions.find((o) => o.resolution === selectedResolution) ?? null;
  const showAggregationPicker =
    showResolutionPicker && selectedResolution !== null && selectedResolution !== TimeResolution.Raw;

  const customizedCount = [seasonalityMode, changepointPriorScale, seasonalityPriorScale, changepointRange].filter(
    Boolean
  ).length;
  const advancedHint = `${HYPERPARAM_COUNT} parametre · ${customizedCount > 0 ? `${customizedCount} özelleştirildi` : 'varsayılan'}`;

  const footerSummary =
    showResolutionPicker && selectedOption
      ? `${RESOLUTION_LABELS[selectedOption.resolution]} · ${formatPoints(selectedOption.estimatedPoints)} nokta ile eğitilecek`
      : scenario === 'rawOnly'
      ? 'Tüm veri ham çözünürlükte kullanılacak.'
      : scenario === 'fetchError'
      ? 'Çözünürlük seçenekleri alınamadı, ham veri ile eğitilecek.'
      : null;

  const buildHyperparameters = (): ProphetHyperparameters | undefined => {
    const hyperparameters: ProphetHyperparameters = {};
    if (seasonalityMode) hyperparameters.seasonalityMode = seasonalityMode;
    if (changepointPriorScale) hyperparameters.changepointPriorScale = Number(changepointPriorScale);
    if (seasonalityPriorScale) hyperparameters.seasonalityPriorScale = Number(seasonalityPriorScale);
    if (changepointRange) hyperparameters.changepointRange = Number(changepointRange);

    return Object.keys(hyperparameters).length > 0 ? hyperparameters : undefined;
  };

  const handleOpenModal = () => {
    setError(null);
    setIsModalOpen(true);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      const resolution = showResolutionPicker && selectedResolution !== null ? selectedResolution : undefined;
      const aggregation = resolution !== undefined && resolution !== TimeResolution.Raw ? selectedAggregation : undefined;
      await trainModel(datasetId, algorithm, buildHyperparameters(), resolution, aggregation);
      setIsModalOpen(false);
      onModelCreated();
      showToast(TOAST_MESSAGES.modelTrainingStarted, 'success');
    } catch (err) {
      setError(getErrorMessage(err, 'Model eğitimi başlatılamadı.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const canStartTraining = scenario !== 'disabled';

  return (
    <div className={styles.form}>
      {disabled && disabledReason && (
        <div className={styles.hint}>{disabledReason}</div>
      )}

      <div className={styles.row}>
        <select
          className={styles.select}
          value={algorithm}
          onChange={(e) => setAlgorithm(e.target.value)}
          disabled={disabled}
        >
          {ALGORITHMS.map((a) => (
            <option key={a.value} value={a.value}>{a.label}</option>
          ))}
        </select>
        <Button type="button" onClick={handleOpenModal} disabled={disabled} style={{ width: 'auto' }}>
          Model Eğit
        </Button>
      </div>

      <Modal
        isOpen={isModalOpen}
        onClose={() => !isSubmitting && setIsModalOpen(false)}
        title="Model Eğitimi"
        className={styles.wideModal}
      >
        <form onSubmit={handleSubmit} className={styles.modalForm}>
          {error && <div className={styles.error}>{error}</div>}

          {scenario === 'loading' && (
            <p className={styles.hint}>Veri seti büyüklüğüne göre uygun seçenekler kontrol ediliyor...</p>
          )}

          {scenario === 'disabled' && (
            <div className={styles.error}>
              Bu veri seti için uygun bir eğitim çözünürlüğü bulunamadı, model eğitimi başlatılamıyor.
            </div>
          )}

          {scenario === 'fetchError' && (
            <p className={styles.infoBox}>
              Çözünürlük seçenekleri alınamadı, model varsayılan (ham veri) ayarıyla eğitilecek.
            </p>
          )}

          {scenario === 'mandatory' && (
            <div className={styles.warningBox}>
              <span className={styles.warningIcon}>!</span>
              <div>
                <p className={styles.warningTitle}>Bu veri seti eğitim için fazla yoğun</p>
                <p className={styles.warningDesc}>
                  Eğitimden önce bir zaman çözünürlüğüne özetlenmesi gerekiyor. Önerilen seçenek otomatik seçildi.
                </p>
              </div>
            </div>
          )}

          {showResolutionPicker && (
            <div className={styles.resolutionSection}>
              <div className={styles.resolutionHeader}>
                <span>Zaman çözünürlüğü</span>
                {selectedOption && (
                  <span className={styles.resolutionHeaderHint}>
                    eğitim verisi: {formatPoints(selectedOption.estimatedPoints)} nokta
                  </span>
                )}
              </div>

              <div className={styles.resolutionList}>
                {dedupedOptions.map((o) => (
                  <label
                    key={o.resolution}
                    className={`${styles.resolutionRow} ${
                      selectedResolution === o.resolution ? styles.resolutionRowSelected : ''
                    }`}
                  >
                    <input
                      type="radio"
                      name="resolution"
                      checked={selectedResolution === o.resolution}
                      onChange={() => setSelectedResolution(o.resolution)}
                      disabled={isSubmitting}
                    />
                    <span className={styles.resolutionRowLabel}>{RESOLUTION_LABELS[o.resolution]}</span>
                    <span className={styles.resolutionRowPoints}>{formatPoints(o.estimatedPoints)} nokta</span>
                    {o.isRecommended && <span className={styles.badge}>ÖNERİLEN</span>}
                  </label>
                ))}
              </div>
            </div>
          )}

          {showAggregationPicker && (
            <div className={styles.aggregationSection}>
              <span className={styles.radioGroupLabel}>Değerler nasıl birleştirilsin?</span>
              <div className={styles.aggregationCards}>
                <label
                  className={`${styles.aggregationCard} ${
                    selectedAggregation === AggregationFunction.Average ? styles.aggregationCardSelected : ''
                  }`}
                >
                  <input
                    type="radio"
                    name="aggregation"
                    checked={selectedAggregation === AggregationFunction.Average}
                    onChange={() => setSelectedAggregation(AggregationFunction.Average)}
                    disabled={isSubmitting}
                  />
                  <span className={styles.aggregationCardTitle}>Ortalama</span>
                  <span className={styles.aggregationCardDesc}>Seviye ölçümü — sıcaklık, fiyat, stok</span>
                </label>
                <label
                  className={`${styles.aggregationCard} ${
                    selectedAggregation === AggregationFunction.Sum ? styles.aggregationCardSelected : ''
                  }`}
                >
                  <input
                    type="radio"
                    name="aggregation"
                    checked={selectedAggregation === AggregationFunction.Sum}
                    onChange={() => setSelectedAggregation(AggregationFunction.Sum)}
                    disabled={isSubmitting}
                  />
                  <span className={styles.aggregationCardTitle}>Toplam</span>
                  <span className={styles.aggregationCardDesc}>Sayım/miktar — satış adedi, ziyaretçi</span>
                </label>
              </div>
            </div>
          )}

          {scenario === 'rawOnly' && rawOption && (
            <div className={styles.statPanel}>
              <div className={styles.statCell}>
                <span className={styles.statLabel}>EĞİTİM VERİSİ</span>
                <span className={styles.statValue}>{formatPoints(rawOption.estimatedPoints)} nokta</span>
              </div>
              <div className={styles.statCell}>
                <span className={styles.statLabel}>ÇÖZÜNÜRLÜK</span>
                <span className={styles.statValue}>Ham</span>
              </div>
            </div>
          )}

          <details className={styles.advanced}>
            <summary className={styles.advancedSummary}>
              <span className={styles.advancedLabel}>
                <LuChevronDown size={14} className={styles.advancedChevron} />
                Gelişmiş seçenekler
              </span>
              <span className={styles.advancedHint}>{advancedHint}</span>
            </summary>

            <div className={styles.advancedGrid}>
              <label className={styles.fieldLabel}>
                Mevsimsellik modu
                <select
                  className={styles.select}
                  value={seasonalityMode}
                  onChange={(e) => setSeasonalityMode(e.target.value as '' | 'additive' | 'multiplicative')}
                  disabled={isSubmitting}
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
                  disabled={isSubmitting}
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
                  disabled={isSubmitting}
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
                  disabled={isSubmitting}
                />
              </label>
            </div>
          </details>

          <div className={styles.footer}>
            <span className={styles.footerSummary}>{footerSummary}</span>
            <div className={styles.footerButtons}>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setIsModalOpen(false)}
                disabled={isSubmitting}
                style={{ width: 'auto' }}
              >
                Vazgeç
              </Button>
              <Button type="submit" disabled={isSubmitting || !canStartTraining} style={{ width: 'auto' }}>
                {isSubmitting ? 'Başlatılıyor...' : 'Eğitimi Başlat'}
              </Button>
            </div>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export default ModelTrainingForm;
