FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Backend/SportsBetting.API/SportsBetting.API.csproj src/Backend/SportsBetting.API/
COPY src/Backend/SportsBetting.Application/SportsBetting.Application.csproj src/Backend/SportsBetting.Application/
COPY src/Backend/SportsBetting.Domain/SportsBetting.Domain.csproj src/Backend/SportsBetting.Domain/
COPY src/Backend/SportsBetting.Infrastructure/SportsBetting.Infrastructure.csproj src/Backend/SportsBetting.Infrastructure/
COPY src/Shared/SportsBetting.Communication/SportsBetting.Communication.csproj src/Shared/SportsBetting.Communication/
COPY src/Shared/SportsBetting.Exceptions/SportsBetting.Exceptions.csproj src/Shared/SportsBetting.Exceptions/

RUN dotnet restore src/Backend/SportsBetting.API/SportsBetting.API.csproj
COPY . .
RUN dotnet publish src/Backend/SportsBetting.API/SportsBetting.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM build AS migrations-build
ENV PATH="$PATH:/root/.dotnet/tools"
ENV SPORTSBETTING_EF_CONNECTION="Server=design-time-placeholder;Database=SportsBetting;User Id=sa;Password=design-time-placeholder;TrustServerCertificate=True;"
RUN dotnet tool install --global dotnet-ef --version 10.0.*
RUN dotnet ef migrations bundle \
    --project src/Backend/SportsBetting.Infrastructure \
    --startup-project src/Backend/SportsBetting.Infrastructure \
    --context SportsBettingDbContext \
    --configuration Release \
    --force \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SportsBetting.API.dll"]
