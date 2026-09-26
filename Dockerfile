FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TimeSeriesForecaster.Domain/*.csproj TimeSeriesForecaster.Domain/
COPY TimeSeriesForecaster.Application/*.csproj TimeSeriesForecaster.Application/
COPY TimeSeriesForecaster.Infrastructure/*.csproj TimeSeriesForecaster.Infrastructure/
COPY TimeSeriesForecaster.WebAPI/*.csproj TimeSeriesForecaster.WebAPI/

RUN dotnet restore TimeSeriesForecaster.WebAPI/TimeSeriesForecaster.WebAPI.csproj

COPY . .

RUN dotnet publish TimeSeriesForecaster.WebAPI/TimeSeriesForecaster.WebAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/uploads /app/logs && chown -R $APP_UID:$APP_UID /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "TimeSeriesForecaster.WebAPI.dll"]