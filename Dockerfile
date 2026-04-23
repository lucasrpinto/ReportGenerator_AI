# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia tudo do repositório
COPY . .

# Restaura e publica a API
RUN dotnet restore "src/Relatorios.Api/Relatorios.Api.csproj"
RUN dotnet publish "src/Relatorios.Api/Relatorios.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Relatorios.Api.dll"]