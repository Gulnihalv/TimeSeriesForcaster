import apiClient from "../../../api/apiClient";

export interface Dataset {
    id: number;
    projectId: number;
    name: string;
    originalFileName: string;
    filePath: string;
    recordCount: number;
    isProcessed: boolean;
    createdAt: string;
}

export const getDatasetsForProject = async (projectId: number): Promise<Dataset[]> => {
  const response = await apiClient.get<Dataset[]>(`/projects/${projectId}/datasets`);
  return response.data;
};

export const createDataset = async (
  projectId: number, 
  name: string, 
  file: File
): Promise<Dataset> => {

  const formData = new FormData();
  formData.append('name', name);
  formData.append('file', file);

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