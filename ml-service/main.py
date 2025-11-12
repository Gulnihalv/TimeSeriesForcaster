import os
from fastapi import FastAPI
import pandas as pd
from pydantic import BaseModel
from prophet import Prophet
from prophet.serialize import model_to_json
import uuid
from typing import List

app = FastAPI()

class TrainingData(BaseModel):
    ds: str
    y: float

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

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)