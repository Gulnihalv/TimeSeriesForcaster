import apiClient from "../../../api/apiClient";

export const ModelStatus = {
  Queued: 0,
  Training: 1,
  Completed: 2,
  Failed: 3,
  Cancelled: 4,
} as const;
export type ModelStatus = (typeof ModelStatus)[keyof typeof ModelStatus];

export const ForecastStatus = {
  Queued: 0,
  Generating: 1,
  Completed: 2,
  Failed: 3,
  Cancelled: 4,
} as const;
export type ForecastStatus = (typeof ForecastStatus)[keyof typeof ForecastStatus];

export const MetricName = {
  MAE: 0,
  RMSE: 1,
} as const;
export type MetricName = (typeof MetricName)[keyof typeof MetricName];

export const TimeResolution = {
  Raw: 0,
  Minute: 1,
  Hour: 2,
  Day: 3,
  Week: 4,
  Month: 5
} as const;
export type TimeResolution = (typeof TimeResolution)[keyof typeof TimeResolution];

export const AggregationFunction = {
  Average: 0,
  Sum: 1,
  None: 2
} as const;
export type AggregationFunction = (typeof AggregationFunction)[keyof typeof AggregationFunction];

export interface Model {
  id: number;
  projectId: number;
  datasetId: number;
  modelName: string;
  algorithm: string;
  hyperparameters: string | null;
  modelFilePath: string | null;
  status: ModelStatus;
  errorMessage: string | null;
  trainingStartedAt: string | null;
  trainingCompletedAt: string | null;
  createdAt: string;
  isActive: boolean;
  forecastStatus: ForecastStatus | null;
  forecastProgressPercentage: number;
  forecastErrorMessage: string | null;
  forecastStartedAt: string | null;
  forecastCompletedAt: string | null;
  trainingResolution: TimeResolution | null;
  trainingAggregation: AggregationFunction | null;
  trainingRowCount: number | null;
}

export interface Prediction {
  id: number;
  predictionDate: string;
  predictedValue: number;
  confidenceLower: number;
  confidenceUpper: number;
  actualValue: number | null;
}

export interface ModelMetric {
  metricName: MetricName;
  metricValue: number;
  calculatedAt: string;
}

export interface ModelDetail extends Model {
  predictions: Prediction[];
  metrics: ModelMetric[];
}

export interface ComponentPoint {
  label: string;
  value: number;
}

export interface ModelComponents {
  trend: ComponentPoint[];
  weekly: ComponentPoint[] | null;
  yearly: ComponentPoint[] | null;
}

export interface ResolutionOption {
  resolution: TimeResolution;
  estimatedPoints: number;
  isAllowed: boolean;
  isRecommended: boolean;
}

export const RESOLUTION_LABELS: Record<TimeResolution, string> = {
  [TimeResolution.Raw]: 'Ham',
  [TimeResolution.Minute]: 'Dakikalık',
  [TimeResolution.Hour]: 'Saatlik',
  [TimeResolution.Day]: 'Günlük',
  [TimeResolution.Week]: 'Haftalık',
  [TimeResolution.Month]: 'Aylık',
};

export const AGGREGATION_LABELS: Record<AggregationFunction, string> = {
  [AggregationFunction.Average]: 'Ortalama',
  [AggregationFunction.Sum]: 'Toplam',
  [AggregationFunction.None]: 'Yok',
};

// Prophet'in onlarca parametresi var, en çok etkisi olan birkaçını sunuyoruz.
// Hepsi opsiyonel - gönderilmezse backend/Prophet kendi varsayılanlarını kullanır.
export interface ProphetHyperparameters {
  seasonalityMode?: 'additive' | 'multiplicative';
  changepointPriorScale?: number;
  seasonalityPriorScale?: number;
  changepointRange?: number;
}

export const trainModel = async (
  datasetId: number,
  algorithm: string = "prophet",
  hyperparameters?: ProphetHyperparameters,
  timeResolution?: TimeResolution,
  aggregationFunction?: AggregationFunction
): Promise<Model> => {
  const response = await apiClient.post<Model>(`/datasets/${datasetId}/models`, {
    algorithm,
    hyperparameters,
    trainingResolution: timeResolution,
    trainingAggregation: aggregationFunction
  });
  return response.data;
};

export const getModelsForDataset = async (datasetId: number): Promise<Model[]> => {
  const response = await apiClient.get<Model[]>(`/datasets/${datasetId}/models`);
  return response.data;
};

export const getModelById = async (modelId: number): Promise<ModelDetail> => {
  const response = await apiClient.get<ModelDetail>(`/models/${modelId}`);
  return response.data;
};

export const generateForecast = async (
  modelId: number,
  horizon: number = 30
): Promise<void> => {
  await apiClient.post(`/models/${modelId}/forecast`, { horizon });
};

export const deleteModel = async (modelId: number): Promise<void> => {
  await apiClient.delete(`/models/${modelId}`);
};

export const getModelComponents = async (modelId: number): Promise<ModelComponents> => {
  const response = await apiClient.get<ModelComponents>(`/models/${modelId}/components`);
  return response.data;
};

export const getResolutionOptions = async (datasetId: number): Promise<ResolutionOption[]> => {
  const response = await apiClient.get<ResolutionOption[]>(`/datasets/${datasetId}/resolution-options`);
  return response.data;
};