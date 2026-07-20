import os
from fastapi import FastAPI, HTTPException
import pandas as pd
from pydantic import BaseModel
from prophet import Prophet
from prophet.serialize import model_to_json, model_from_json
import uuid
from typing import List, Optional

app = FastAPI()

class TrainingData(BaseModel):
    ds: str
    y: float

class ProphetHyperparameters(BaseModel):
    seasonality_mode: Optional[str] = None
    changepoint_prior_scale: Optional[float] = None
    seasonality_prior_scale: Optional[float] = None
    changepoint_range: Optional[float] = None

class TrainRequest(BaseModel):
    data: List[TrainingData]
    hyperparameters: Optional[ProphetHyperparameters] = None

class PredictRequest(BaseModel):
    model_path: str
    horizon: int  # kaç adım (gün) ileriye tahmin üretilecek

@app.post("/train/prophet")
def trainModel(request: TrainRequest):

    trainingData = [datapoint.model_dump() for datapoint in request.data]
    df = pd.DataFrame(trainingData)
    df['ds'] = pd.to_datetime(df['ds'])
    if df['ds'].dt.tz is not None:
        df['ds'] = df['ds'].dt.tz_localize(None)  # Prophet tz-aware 'ds' kolonunu kabul etmiyor
    df = df.sort_values('ds').reset_index(drop=True)

    # Gönderilmeyen (None) hiperparametreleri Prophet'e hiç geçirmiyoruz ki
    # Prophet kendi varsayılanlarını kullanabilsin.
    prophet_kwargs = {}
    if request.hyperparameters:
        for field_name, value in request.hyperparameters.model_dump().items():
            if value is not None:
                prophet_kwargs[field_name] = value

    # Holdout tabanlı metrik hesaplama: yeterli veri varsa son ~%15'i test için ayır.
    # Az veri noktalı (özellikle demo/ilk test) senaryolarında holdout yapmak anlamsız/kırılgan
    # olacağından minimum 10 nokta şartı koyuyoruz; altındaysa metrics null döner.
    metrics = None
    n = len(df)
    if n >= 10:
        holdout_size = max(1, int(n * 0.15))
        train_df = df.iloc[:-holdout_size]
        test_df = df.iloc[-holdout_size:]

        holdout_model = Prophet(**prophet_kwargs)
        holdout_model.fit(train_df)

        holdout_forecast = holdout_model.predict(test_df[['ds']])

        errors = holdout_forecast['yhat'].values - test_df['y'].values
        mae = float(abs(errors).mean())
        rmse = float((errors ** 2).mean() ** 0.5)
        metrics = {"mae": mae, "rmse": rmse}

    # Üretim modeli: metrikler ne olursa olsun TÜM veriyle eğitilir (holdout sadece ölçüm içindi).
    # Aynı hiperparametreler burada da kullanılıyor - holdout ile üretim modeli arasındaki tek
    # fark eğitim verisinin kapsamı, ayarlar aynı kalmalı.
    prophet = Prophet(**prophet_kwargs)
    prophet.fit(df)

    model_directory = "models"
    if not os.path.exists(model_directory):
        os.makedirs(model_directory)

    model_id = str(uuid.uuid4())
    model_path = f"{model_directory}/{model_id}.json"
    with open(model_path, 'w') as fout:
        fout.write(model_to_json(prophet)) 

    return { "success": True, "model_path": model_path, "metrics": metrics }

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

class ComponentsRequest(BaseModel):
    model_path: str

@app.post("/components/prophet")
def getComponents(request: ComponentsRequest):
    if not os.path.exists(request.model_path):
        raise HTTPException(status_code=404, detail="Belirtilen model dosyası bulunamadı.")

    with open(request.model_path, 'r') as fin:
        prophet = model_from_json(fin.read())

    # Geleceğe değil, eğitim verisinin kapsadığı tarihlere göre bileşenleri hesaplıyoruz
    history = prophet.history[['ds']].copy()
    forecast = prophet.predict(history)

    def to_points(df, label_col, value_col):
        result = df[[label_col, value_col]].copy()
        result[label_col] = result[label_col].astype(str)
        return result.rename(columns={label_col: 'label', value_col: 'value'}).to_dict(orient='records')

    trend_df = forecast[['ds', 'trend']].copy()
    trend_df['ds'] = trend_df['ds'].astype(str)
    trend = to_points(trend_df, 'ds', 'trend')

    # Haftalık pattern: haftanın her günü (0=Pazartesi..6=Pazar) için ortalama etkiyi hesaplıyoruz,
    # tüm geçmiş satırları değil - 7 noktalık okunabilir bir özet istiyoruz.
    weekly = None
    if 'weekly' in forecast.columns:
        weekly_df = forecast[['ds', 'weekly']].copy()
        weekly_df['day_of_week'] = pd.to_datetime(weekly_df['ds']).dt.dayofweek
        weekly_avg = weekly_df.groupby('day_of_week')['weekly'].mean().reset_index().sort_values('day_of_week')
        weekly = [{"label": str(int(row['day_of_week'])), "value": row['weekly']} for _, row in weekly_avg.iterrows()]

    # Yıllık pattern: az veri noktalı dataset'lerde Prophet bunu hiç üretmeyebilir (normal durum).
    yearly = None
    if 'yearly' in forecast.columns:
        yearly_df = forecast[['ds', 'yearly']].copy()
        yearly_df['day_of_year'] = pd.to_datetime(yearly_df['ds']).dt.dayofyear
        yearly_avg = yearly_df.groupby('day_of_year')['yearly'].mean().reset_index().sort_values('day_of_year')
        yearly_avg = yearly_avg.iloc[::5]  # 365 nokta yerine örnekleyerek grafiği okunur tutuyoruz
        yearly = [{"label": str(int(row['day_of_year'])), "value": row['yearly']} for _, row in yearly_avg.iterrows()]

    return {
        "success": True,
        "trend": trend,
        "weekly": weekly,
        "yearly": yearly,
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)