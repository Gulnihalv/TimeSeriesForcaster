import { useParams } from 'react-router-dom';

const DatasetDetailPage = () => {
  const { datasetId } = useParams();

  return (
    <div>
      <h2>Dataset Detay Sayfası (ID: {datasetId})</h2>
      <p>Model eğitme, sonuçlar ve tahminler ileride burada olacak.</p>
    </div>
  );
};

export default DatasetDetailPage;