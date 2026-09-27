# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as a distinct layer for better caching.
COPY Directory.Build.props ./
COPY ClaimsManagementSystem.sln ./
COPY src/Claims.Domain/Claims.Domain.csproj           src/Claims.Domain/
COPY src/Claims.Shared/Claims.Shared.csproj           src/Claims.Shared/
COPY src/Claims.Application/Claims.Application.csproj  src/Claims.Application/
COPY src/Claims.Infrastructure/Claims.Infrastructure.csproj src/Claims.Infrastructure/
COPY src/Claims.API/Claims.API.csproj                 src/Claims.API/
COPY tests/Claims.Tests/Claims.Tests.csproj           tests/Claims.Tests/
RUN dotnet restore src/Claims.API/Claims.API.csproj

# Copy the rest and publish.
COPY . .
RUN dotnet publish src/Claims.API/Claims.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Writable dirs for claim documents and (when using the free SQLite provider)
# the embedded database file.
RUN mkdir -p /app/claim-documents /app/data
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Claims.API.dll"]
