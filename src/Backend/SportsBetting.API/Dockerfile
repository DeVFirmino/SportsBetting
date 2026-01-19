FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Backend/SportsBetting.API/SportsBetting.API.csproj", "src/Backend/SportsBetting.API/"]
COPY ["src/Backend/SportsBetting.Application/SportsBetting.Application.csproj", "src/Backend/SportsBetting.Application/"]
COPY ["src/Shared/SportsBetting.Communication/SportsBetting.Communication.csproj", "src/Shared/SportsBetting.Communication/"]
COPY ["src/Shared/SportsBetting.Exceptions/SportsBetting.Exceptions.csproj", "src/Shared/SportsBetting.Exceptions/"]
COPY ["src/Backend/SportsBetting.Domain/SportsBetting.Domain.csproj", "src/Backend/SportsBetting.Domain/"]
COPY ["src/Backend/SportsBetting.Infrastructure/SportsBetting.Infrastructure.csproj", "src/Backend/SportsBetting.Infrastructure/"]
RUN dotnet restore "src/Backend/SportsBetting.API/SportsBetting.API.csproj"
COPY . .
WORKDIR "/src/src/Backend/SportsBetting.API"
RUN dotnet build "./SportsBetting.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SportsBetting.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SportsBetting.API.dll"]
