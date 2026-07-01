FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["KRD.AttendanceWeb.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /app/data
ENV KRD_DB_PATH=/app/data/KRDAttendanceWeb.db

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KRD.AttendanceWeb.dll"]
