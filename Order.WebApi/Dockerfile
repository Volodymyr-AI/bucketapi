# --- Base stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy sln and projects
COPY orderbackend.sln ./
COPY Domain/ ./Domain/
COPY Infrastructure/ ./Infrastructure/
COPY Presentation/ ./Presentation/

# Restore and publish
RUN dotnet restore orderbackend.sln
RUN dotnet publish Presentation/Order.WebApi/Order.WebApi.csproj -c Release -o /app/publish

# --- Final stage ---
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Order.WebApi.dll"]