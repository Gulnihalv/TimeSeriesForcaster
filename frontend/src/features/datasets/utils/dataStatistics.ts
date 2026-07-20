import type { DataPoint } from '../api/datasetApi';

export interface DatasetStatistics {
  mean: number;
  median: number;
  stdDev: number;
  min: number;
  max: number;
  minDate: string;
  maxDate: string;
  coefficientOfVariation: number | null; // ortalama 0 ise bölme hatası olmasın diye null
}

export const calculateStatistics = (dataPoints: DataPoint[]): DatasetStatistics | null => {
  if (!dataPoints || dataPoints.length === 0) return null;

  const values = dataPoints.map((dp) => dp.value);
  const n = values.length;

  const mean = values.reduce((sum, v) => sum + v, 0) / n;

  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(n / 2);
  const median = n % 2 === 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];

  const variance = values.reduce((sum, v) => sum + (v - mean) ** 2, 0) / n;
  const stdDev = Math.sqrt(variance);

  let minPoint = dataPoints[0];
  let maxPoint = dataPoints[0];
  for (const dp of dataPoints) {
    if (dp.value < minPoint.value) minPoint = dp;
    if (dp.value > maxPoint.value) maxPoint = dp;
  }

  const coefficientOfVariation = mean !== 0 ? stdDev / Math.abs(mean) : null;

  return {
    mean,
    median,
    stdDev,
    min: minPoint.value,
    max: maxPoint.value,
    minDate: minPoint.timestamp,
    maxDate: maxPoint.timestamp,
    coefficientOfVariation,
  };
};

export const describeVariability = (cv: number | null): string => {
  if (cv === null) return '—';
  if (cv < 0.15) return 'Düşük değişkenlik';
  if (cv < 0.35) return 'Orta değişkenlik';
  return 'Yüksek değişkenlik';
};