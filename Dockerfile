FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Gepa.Net.Api/Gepa.Net.Api.csproj", "src/Gepa.Net.Api/"]
RUN dotnet restore "src/Gepa.Net.Api/Gepa.Net.Api.csproj"
COPY . .
WORKDIR "/src/src/Gepa.Net.Api"
RUN dotnet build "Gepa.Net.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gepa.Net.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
	CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Gepa.Net.Api.dll"]
