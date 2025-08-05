# --- Base stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy sln and projects
COPY orderbackend.sln ./
COPY Order.Core/ ./Order.Core/
COPY Order.Application/ ./Order.Application/
COPY Order.Infrastructure/ ./Order.Infrastructure/
COPY Order.WebApi/ ./Order.WebApi/

# Restore and publish
RUN dotnet restore orderbackend.sln
RUN dotnet publish Order.WebApi/Order.WebApi.csproj -c Release -o /app/publish

# --- Final stage ---
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Order.WebApi.dll"]