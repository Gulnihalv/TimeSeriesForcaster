import React, { useState } from 'react';
import Input from '../../../components/Input/Input';
import Button from '../../../components/Button/Button';
import { createDataset } from '../api/datasetApi';
import { getErrorMessage } from '../../../api/errorUtils';
import styles from './CreateDatasetForm.module.css';

interface CreateDatasetFormProps {
  projectId: number;
  onDatasetCreated: () => void;
}

const CreateDatasetForm: React.FC<CreateDatasetFormProps> = ({ projectId, onDatasetCreated }) => {
  const [name, setName] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [dateColumnName, setDateColumnName] = useState('');
  const [targetColumnName, setTargetColumnName] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0]);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!file) {
      setError('Lütfen bir CSV dosyası seçin.');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await createDataset(projectId, name, file, dateColumnName, targetColumnName);
      setIsLoading(false);
      onDatasetCreated();
    } catch (err) {
      setIsLoading(false);
      // Backend artık anlamlı hatalar dönüyor (örn. "CSV dosyasında şu sütun(lar) bulunamadı: ...")
      setError(getErrorMessage(err, 'Dataset yüklenemedi. Lütfen tekrar deneyin.'));
      console.error('Dataset yükleme hatası:', err);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {error && <div className={styles.error}>{error}</div>}

      <div className={styles.formGroup}>
        <label htmlFor="datasetName">Dataset Adı</label>
        <Input
          type="text"
          id="datasetName"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
      </div>

      <div className={styles.formGroup}>
        <label htmlFor="file">CSV Dosyası</label>
        {/* 6. Normal Input'umuz 'file' tipinde */}
        <Input
          type="file"
          id="file"
          onChange={handleFileChange}
          accept=".csv" // Sadece .csv dosyalarına izin ver
          required
        />
      </div>

      <div className={styles.formGroup}>
        <label htmlFor="dateColumnName">Tarih Sütunu Adı</label>
        <Input
          type="text"
          id="dateColumnName"
          value={dateColumnName}
          onChange={(e) => setDateColumnName(e.target.value)}
          placeholder="örn. tarih"
          required
        />
      </div>

      <div className={styles.formGroup}>
        <label htmlFor="targetColumnName">Hedef Değer Sütunu Adı</label>
        <Input
          type="text"
          id="targetColumnName"
          value={targetColumnName}
          onChange={(e) => setTargetColumnName(e.target.value)}
          placeholder="örn. satis"
          required
        />
      </div>

      <Button type="submit" disabled={isLoading}>
        {isLoading ? 'Yükleniyor...' : 'Dataset Yükle'}
      </Button>
    </form>
  );
};

export default CreateDatasetForm;