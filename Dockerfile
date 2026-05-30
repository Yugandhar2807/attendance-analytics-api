FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore (layer-cached)
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Attendance.Api/Attendance.Api.csproj", "src/Attendance.Api/"]
COPY ["src/Attendance.Application/Attendance.Application.csproj", "src/Attendance.Application/"]
COPY ["src/Attendance.Domain/Attendance.Domain.csproj", "src/Attendance.Domain/"]
COPY ["src/Attendance.Infrastructure/Attendance.Infrastructure.csproj", "src/Attendance.Infrastructure/"]
RUN dotnet restore "src/Attendance.Api/Attendance.Api.csproj"

# Build + publish
COPY . .
WORKDIR /src/src/Attendance.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Attendance.Api.dll"]
