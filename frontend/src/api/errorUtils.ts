import axios from "axios";

// Backend hataları düz string olarak dönüyor (örn. BadRequest(ex.Message)).
// Bu fonksiyon axios hatasından güvenli şekilde kullanıcıya gösterilebilir bir mesaj çıkarır.
export const getErrorMessage = (error: unknown, fallback = "Beklenmeyen bir hata oluştu."): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;
    if (typeof data === "string" && data.length > 0) {
      return data;
    }
    if (data && typeof data === "object" && "message" in data && typeof (data as { message?: unknown }).message === "string") {
      return (data as { message: string }).message;
    }
    return error.message || fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
};