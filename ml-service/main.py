import os
from fastapi import FastAPI, HTTPException
import pandas as pd
from pydantic import BaseModel
from prophet import Prophet
from prophet.serialize import model_to_json, model_from_json
import uuid
from typing import List

app = FastAPI()

class TrainingData(BaseModel):
    ds: str
    y: float

class PredictRequest(BaseModel):
    model_path: str
    horizon: int  # kaç adım (gün) ileriye tahmin üretilecek

@app.post("/train/prophet")
def trainModel(data: List[TrainingData]):
    
    trainingData = [datapoint.model_dump() for datapoint in data]
    df = pd.DataFrame(trainingData)
    prophet = Prophet()
    prophet.fit(df)

    model_directory = "models"
    if not os.path.exists(model_directory):
        os.makedirs(model_directory)

    model_id = str(uuid.uuid4())
    model_path = f"{model_directory}/{model_id}.json"
    with open(model_path, 'w') as fout:
        fout.write(model_to_json(prophet)) 

    return { "success": True, "model_path": model_path}

@app.post("/predict/prophet")
def predictModel(request: PredictRequest):
    if not os.path.exists(request.model_path):
        raise HTTPException(status_code=404, detail="Belirtilen model dosyası bulunamadı.")

    if request.horizon <= 0:
        raise HTTPException(status_code=400, detail="horizon 0'dan büyük olmalı.")

    with open(request.model_path, 'r') as fin:
        prophet = model_from_json(fin.read())

    # Eğitim setindeki son tarih - bunun sonrasını "gerçek gelecek" sayıyoruz
    last_historical_date = prophet.history_dates.max()

    future = prophet.make_future_dataframe(periods=request.horizon)
    forecast = prophet.predict(future)

    future_only = forecast[forecast['ds'] > last_historical_date].copy()
    future_only['ds'] = future_only['ds'].dt.strftime('%Y-%m-%dT%H:%M:%S')

    result = future_only[['ds', 'yhat', 'yhat_lower', 'yhat_upper']].to_dict(orient='records')

    return { "success": True, "predictions": result }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)