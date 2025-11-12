from fastapi import FastAPI

app = FastAPI()

@app.get("/")
def root():
    return {"message": "Hello Gulnihalllllllll ins calisirr"}

@app.post("/train/prophet")
def 