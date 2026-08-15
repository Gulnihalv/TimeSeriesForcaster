export const describeVariability = (cv: number | null): string => {
  if (cv === null) return '—';
  if (cv < 0.15) return 'Düşük değişkenlik';
  if (cv < 0.35) return 'Orta değişkenlik';
  return 'Yüksek değişkenlik';
};