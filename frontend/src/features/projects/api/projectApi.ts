import apiClient from "../../../api/apiClient";

export interface Project{
    id: number;
    userId: number;
    name: string;
    description: string;
    createdAt: string;
    isActive: boolean;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
}

export const getProjects = async (): Promise<Project[]> => {
    const response = await apiClient.get<Project[]>('/projects');
    return response.data;
}

export const createProject = async (data: CreateProjectRequest): Promise<Project> => {
  const response = await apiClient.post<Project>('/projects', data);
  return response.data;
};