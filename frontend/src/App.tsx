import { useState, useEffect } from 'react';
import axios from 'axios';
import './App.css';

interface Test {
  message: string;
}

function App() {
  const [message, setMessage] = useState<Test>({ message: '' });

  const API_URL = 'http://localhost:5131/api/test/ping';

  useEffect(() => {
    axios.get(API_URL)
      .then(response => {
        setMessage(response.data);
      })
      .catch(err => {
        console.error("API isteği sırasında hata oluştu!", err);
      });
  }, []); 

  return (
    <>
      <h1>Akıllı Zaman Serisi Platformu</h1>
      <h2>Ping Pong Deneme</h2>
      <ul>
        message: {message.message ? (<p> {message.message}</p>) : (<p>Yükleniyor...</p>)}
      </ul>
    </>
  );
}

export default App;