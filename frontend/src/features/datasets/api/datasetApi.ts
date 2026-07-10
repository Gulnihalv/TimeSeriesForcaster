import apiClient from "../../../api/apiClient";

export interface Dataset {
    id: number;
    projectId: number;
    name: string;
    originalFileName: string;
    filePath: string;
    dateColumn: string | null;
    targetColumn: string | null;
    recordCount: number;
    startDate: string | null;
    endDate: string | null;
    isProcessed: boolean;
    errorMessage: string | null;
    isActive: boolean;
    createdAt: string;
}

export const getDatasetsForProject = async (projectId: number): Promise<Dataset[]> => {
  const response = await apiClient.get<Dataset[]>(`/projects/${projectId}/datasets`);
  return response.data;
};

export const getDatasetById = async (datasetId: number): Promise<Dataset> => {
  const response = await apiClient.get<Dataset>(`/datasets/${datasetId}`);
  return response.data;
};

export const createDataset = async (
  projectId: number,
  name: string,
  file: File,
  dateColumnName: string,
  targetColumnName: string
): Promise<Dataset> => {

  const formData = new FormData();
  formData.append('name', name);
  formData.append('file', file);
  formData.append('dateColumnName', dateColumnName);
  formData.append('targetColumnName', targetColumnName);

  const response = await apiClient.post<Dataset>(
    `/projects/${projectId}/datasets/upload`,
    formData,
    {
      headers: {
        'Content-Type': undefined,
      },
    }
  );

  return response.data;
};