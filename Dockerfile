# --- Aşama 1: Derle (SDK kutusu) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/PaymentGateway.Api/*.csproj ./PaymentGateway.Api/
RUN dotnet restore PaymentGateway.Api/PaymentGateway.Api.csproj
COPY src/PaymentGateway.Api/ ./PaymentGateway.Api/
RUN dotnet publish PaymentGateway.Api/PaymentGateway.Api.csproj -c Release -o /app

# --- Aşama 2: Çalıştır (küçük runtime kutusu) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]