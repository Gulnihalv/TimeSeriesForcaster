import axios from "axios";
import { useAuthStore } from "../store/authStore";

const apiClient = axios.create({
  baseURL: 'http://localhost:5131/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
    const token = useAuthStore.getState().token;
    if (token){
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

const AUTH_ENDPOINTS = ['/authentication/login', '/authentication/register'];

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => error.config?.url?.includes(path));

    if (error.response?.status === 401 && !isAuthEndpoint) {
      useAuthStore.getState().logout();
      window.location.href = '/login';
    }

    return Promise.reject(error);
  }
);

export default apiClient;