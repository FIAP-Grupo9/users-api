# Build context = raiz deste repositorio
# Build: docker build -t fcg-users-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia primeiro sÃ³ os csproj e restaura â€” aproveita cache do Docker
COPY shared/FCG.Contracts/FCG.Contracts.csproj shared/FCG.Contracts/
COPY shared/FCG.BuildingBlocks/FCG.BuildingBlocks.csproj shared/FCG.BuildingBlocks/
COPY src/FCG.Users.Api/FCG.Users.Api.csproj src/FCG.Users.Api/
RUN dotnet restore src/FCG.Users.Api/FCG.Users.Api.csproj

# Copia o resto e publica
COPY shared/ shared/
COPY . .
RUN dotnet publish src/FCG.Users.Api/FCG.Users.Api.csproj \
    -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FCG.Users.Api.dll"]
