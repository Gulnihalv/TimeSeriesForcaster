import apiClient from "../../../api/apiClient";

export const ModelStatus = {
  Queued: 0,
  Training: 1,
  Completed: 2,
  Failed: 3,
} as const;
export type ModelStatus = (typeof ModelStatus)[keyof typeof ModelStatus];

export const MetricName = {
  MAE: 0,
  RMSE: 1,
} as const;
export type MetricName = (typeof MetricName)[keyof typeof MetricName];

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
  hyperparameters?: ProphetHyperparameters
): Promise<Model> => {
  const response = await apiClient.post<Model>(`/datasets/${datasetId}/models`, {
    algorithm,
    hyperparameters,
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
  // Backend 202 Accepted dönüyor - iş Hangfire'a kuyruklandı, henüz sonuç hazır değil.
  // Sonucu görmek için getModelById ile polling yapılması gerekir.
  await apiClient.post(`/models/${modelId}/forecast`, { horizon });
};

export const deleteModel = async (modelId: number): Promise<void> => {
  await apiClient.delete(`/models/${modelId}`);
};

export const getModelComponents = async (modelId: number): Promise<ModelComponents> => {
  const response = await apiClient.get<ModelComponents>(`/models/${modelId}/components`);
  return response.data;
};