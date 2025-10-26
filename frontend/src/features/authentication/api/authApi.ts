import apiClient from "../../../api/apiClient";
import { useAuthStore } from "../../../store/authStore";
import { jwtDecode } from 'jwt-decode';

//login bölümü
interface LoginRequest{
    email: string,
    password: string
}

interface LoginResponse {
  token: string;
}

interface DecodedToken {
  nameid: string;
  email: string;
  firstname: string;
  lastname: string;
  exp: number;
  iss: string; // Issuer
  aud: string; // Audience
}

export const login = async (data: LoginRequest) => {
  const response = await apiClient.post<LoginResponse>('/authentication/login', data);
  const {token} = response.data;

  const decodedUser: DecodedToken = jwtDecode(token);

  useAuthStore.getState().login(token, {
    email: decodedUser.email,
    firstName: decodedUser.firstname,
  });

  return response.data;

}

// register bölümü
interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

interface RegisterResponse {
  email: string;
  firstName: string;
  lastName: string;
}

export const register = async (data: RegisterRequest) => {
    const response = await apiClient.post<RegisterResponse>('/authentication/register', data);

    return response.data;
}