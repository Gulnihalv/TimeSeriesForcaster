import axios from "axios";

// Backend hataları iki farklı şekilde dönebilir:
// 1) Düz string (çoğu controller: BadRequest(ex.Message) gibi)
// 2) ProblemDetails (RFC 7807) - global exception handler'ın yakaladığı beklenmeyen hatalar için
export const getErrorMessage = (error: unknown, fallback = "Beklenmeyen bir hata oluştu."): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;

    if (typeof data === "string" && data.length > 0) {
      return data;
    }

    if (data && typeof data === "object") {
      if ("detail" in data && typeof (data as { detail?: unknown }).detail === "string") {
        return (data as { detail: string }).detail;
      }
      if ("title" in data && typeof (data as { title?: unknown }).title === "string") {
        return (data as { title: string }).title;
      }
      if ("message" in data && typeof (data as { message?: unknown }).message === "string") {
        return (data as { message: string }).message;
      }
    }

    return error.message || fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
};