# Dockerfile — Lost & Found Application
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LostAndFoundApp.csproj", "./"]
RUN dotnet restore "LostAndFoundApp.csproj"
COPY . .
RUN dotnet publish "LostAndFoundApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Serve
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create secure storage directories for file uploads
# NOTE: On ephemeral hosts (e.g., Render.com free tier), these directories are lost on redeploy.
# For persistent storage, mount a volume at /app/SecureStorage or use cloud storage (S3/Azure Blob).
RUN mkdir -p /app/SecureStorage/Photos /app/SecureStorage/Attachments \
    && mkdir -p /app/Logs

# Render.com dynamic PORT binding
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Health check — ensures the container is actually serving requests
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/Account/Login || exit 1

ENTRYPOINT ["dotnet", "LostAndFoundApp.dll"]
