# Dockerfile — Bust cache for migration fix
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
# Render.com dynamic PORT binding
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LostAndFoundApp.dll"]
