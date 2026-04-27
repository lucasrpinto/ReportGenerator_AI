# Dockerfile

# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia todos os arquivos do projeto
COPY . .

# Restaura os pacotes da API
RUN dotnet restore "src/Relatorios.Api/Relatorios.Api.csproj"

# Publica somente a API
RUN dotnet publish "src/Relatorios.Api/Relatorios.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime da API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copia os arquivos publicados
COPY --from=build /app/publish .

# Porta usada pelo container
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Inicia a API
ENTRYPOINT ["dotnet", "Relatorios.Api.dll"]