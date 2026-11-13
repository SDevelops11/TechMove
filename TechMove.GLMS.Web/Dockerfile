# Multi-stage Dockerfile for TechMove.GLMS.Web (MVC Frontend Client)
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
COPY ["TechMove.GLMS.Web/TechMove.GLMS.Web.csproj", "TechMove.GLMS.Web/"]
RUN dotnet restore "TechMove.GLMS.Web/TechMove.GLMS.Web.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/TechMove.GLMS.Web"
RUN dotnet build "TechMove.GLMS.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish Executable Artifacts
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TechMove.GLMS.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Production Runtime Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechMove.GLMS.Web.dll"]

