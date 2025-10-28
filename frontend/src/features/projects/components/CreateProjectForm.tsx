import React, { useState } from 'react';
import Input from '../../../components/Input/Input';
import Button from '../../../components/Button/Button';
import { createProject } from '../api/projectApi';
import styles from './CreateProjectForm.module.css';

interface CreateProjectFormProps{
    onProjectCreated: () => void;
}

const CreateProjectForm: React.FC<CreateProjectFormProps> = ({onProjectCreated}) => {
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const  handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError(null);

        try {
            await createProject({name, description});
            setIsLoading(false);
            onProjectCreated(); // dashboarada başarılı proje üretimesini haber vermek için
        }
        catch (err){
            setIsLoading(false);
            setError("Proje oluşturulamadı. Lütfen tekrar deneyin");
            console.error("Proje oluşturulamadı", err);
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            {error && <div className={styles.error}>{error}</div>}

            <div className={styles.formGroup}>
              <label htmlFor="projectName">Proje Adı</label>
              <Input
                type="text"
                id="projectName"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>

            <div className={styles.formGroup}>
              <label htmlFor="projectDesc">Açıklama</label>
              <Input
                type="text"
                id="projectDesc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Oluşturuluyor...' : 'Proje Oluştur'}
            </Button>
        </form>
    );
};

export default CreateProjectForm;