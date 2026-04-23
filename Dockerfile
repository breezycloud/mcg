# ─── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install build-time system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    python3 \
    && rm -rf /var/lib/apt/lists/*

# Install Blazor WebAssembly tooling (needed for Client project)
RUN dotnet workload install wasm-tools

# Copy solution + project files first so dependency restore is cached
# as a separate layer — only re-runs when .csproj / .sln files change
COPY ["mcg.sln", "."]
COPY ["Api/Api.csproj", "Api/"]
COPY ["Client/Client.csproj", "Client/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore

# Copy all source and publish
COPY . .
RUN dotnet publish "Api/Api.csproj" -c Release -o /app/publish --no-restore


# ─── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Runtime dependencies:
#   libgdiplus → System.Drawing native support
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Directory for uploaded files (mirrors appsettings FileStorage:UploadPath)
RUN mkdir -p /var/www/uploads && chmod 755 /var/www/uploads

COPY --from=build /app/publish .

EXPOSE 5294 7229

CMD ["dotnet", "Api.dll"]
