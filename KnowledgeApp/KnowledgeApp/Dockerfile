# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0.100 AS build
WORKDIR /src

COPY ../../ ./

RUN dotnet restore "KnowledgeApp/KnowledgeApp.sln"
RUN dotnet publish "KnowledgeApp/KnowledgeApp/KnowledgeApp.API.csproj" -c Release -o /app/publish

# --- Stage 2: Run ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KnowledgeApp.dll"]
