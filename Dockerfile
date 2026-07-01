# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Use global.json to lock SDK version
COPY ["KRD.AttendanceWeb.csproj", "."]

# Downgrade target framework for Docker compatibility
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV KRD_DB_PATH=/app/data/KRDAttendanceWeb.db

RUN mkdir -p /app/data

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KRD.AttendanceWeb.dll"]
