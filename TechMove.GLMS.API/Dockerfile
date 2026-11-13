# Multi-stage Dockerfile for TechMove.GLMS.API
# Stage 1: Base Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# Stage 2: SDK Build Environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies (efficient Docker cache layering)
COPY ["TechMove.GLMS.API/TechMove.GLMS.API.csproj", "TechMove.GLMS.API/"]
RUN dotnet restore "TechMove.GLMS.API/TechMove.GLMS.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/TechMove.GLMS.API"
RUN dotnet build "TechMove.GLMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish Executable Artifacts
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TechMove.GLMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Production Runtime Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechMove.GLMS.API.dll"]

