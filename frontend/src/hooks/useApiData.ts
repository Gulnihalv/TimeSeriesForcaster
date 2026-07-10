import { useCallback, useEffect, useRef, useState, type DependencyList } from 'react';
import { getErrorMessage } from '../api/errorUtils';

interface UseApiDataOptions<T> {
  /** Veri geldikten sonra hâlâ poll edilmeye devam edilsin mi? (örn. model durumu "Training" olduğu sürece) */
  shouldPoll?: (data: T) => boolean;
  /** Poll aralığı (ms). Varsayılan 3000. */
  pollIntervalMs?: number;
  /** Hata mesajı özelleştirmek için */
  fallbackErrorMessage?: string;
}

interface UseApiDataResult<T> {
  data: T | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useApiData<T>(
  fetchFn: () => Promise<T>,
  deps: DependencyList,
  options: UseApiDataOptions<T> = {}
): UseApiDataResult<T> {
  const { shouldPoll, pollIntervalMs = 3000, fallbackErrorMessage } = options;

  const [data, setData] = useState<T | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const fetchFnRef = useRef(fetchFn);
  fetchFnRef.current = fetchFn;

  const load = useCallback(async (isBackgroundRefresh = false) => {
    if (!isBackgroundRefresh) {
      setIsLoading(true);
      setError(null);
    }
    try {
      const result = await fetchFnRef.current();
      setData(result);
      if (!isBackgroundRefresh) setIsLoading(false);
      return result;
    } catch (err) {
      setError(getErrorMessage(err, fallbackErrorMessage));
      if (!isBackgroundRefresh) setIsLoading(false);
      return null;
    }
  }, [fallbackErrorMessage]);

  useEffect(() => {
    load(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    if (!shouldPoll || data === null || !shouldPoll(data)) {
      return;
    }

    intervalRef.current = setInterval(() => {
      load(true);
    }, pollIntervalMs);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, shouldPoll, pollIntervalMs]);

  const refetch = useCallback(() => {
    load(false);
  }, [load]);

  return { data, isLoading, error, refetch };
}
