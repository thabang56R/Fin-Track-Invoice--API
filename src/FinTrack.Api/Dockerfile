# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj files first for better caching
COPY src/FinTrack.Api/FinTrack.csproj src/FinTrack.Api/
COPY src/FinTrack.Infrastructure/FinTrack.Infrastructure.csproj src/FinTrack.Infrastructure/
COPY src/FinTrack.Application/FinTrack.Application.csproj src/FinTrack.Application/
COPY src/FinTrack.Domain/FinTrack.Domain.csproj src/FinTrack.Domain/

RUN dotnet restore src/FinTrack.Api/FinTrack.csproj

# copy everything else
COPY . .

RUN dotnet publish src/FinTrack.Api/FinTrack.csproj -c Release -o /app/publish

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FinTrack.dll"]
