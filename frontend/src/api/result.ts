import { getErrorMessage } from './errorUtils';

export interface ResultSuccess<T> {
  success: true;
  data: T;
}

export interface ResultFailure {
  success: false;
  error: string;
}

export type Result<T> = ResultSuccess<T> | ResultFailure;

/**
 * Bir API çağrısını (Promise) try/catch tekrarı olmadan kullanılabilir bir Result'a çevirir.
 * Backend'deki Result<T> pattern'inin frontend karşılığı.
 */
export const toResult = async <T>(
  promise: Promise<T>,
  fallbackErrorMessage = 'Beklenmeyen bir hata oluştu.'
): Promise<Result<T>> => {
  try {
    const data = await promise;
    return { success: true, data };
  } catch (err) {
    return { success: false, error: getErrorMessage(err, fallbackErrorMessage) };
  }
};