# Stage 1: Build the ASP.NET Core application with the .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the project file first so NuGet restore can be cached
COPY ["FTLSV2/FTLSV2.csproj", "FTLSV2/"]
RUN dotnet restore "FTLSV2/FTLSV2.csproj"

# Copy the remaining source code
COPY . .

WORKDIR /src/FTLSV2

# Publish a production-ready build
RUN dotnet publish "FTLSV2.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Run the published application with the smaller ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Railway provides the PORT environment variable at runtime.
# Fall back to port 8080 for local container testing.
CMD ["sh", "-c", "dotnet FTLSV2.dll --urls http://0.0.0.0:${PORT:-8080}"]
