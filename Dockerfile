# Build stage: restores and publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SportsBetting.sln ./
COPY src/Backend/SportsBetting.API/SportsBetting.API.csproj src/Backend/SportsBetting.API/
COPY src/Backend/SportsBetting.Application/SportsBetting.Application.csproj src/Backend/SportsBetting.Application/
COPY src/Backend/SportsBetting.Domain/SportsBetting.Domain.csproj src/Backend/SportsBetting.Domain/
COPY src/Backend/SportsBetting.Infrastructure/SportsBetting.Infrastructure.csproj src/Backend/SportsBetting.Infrastructure/
COPY src/Shared/SportsBetting.Communication/SportsBetting.Communication.csproj src/Shared/SportsBetting.Communication/
COPY src/Shared/SportsBetting.Exceptions/SportsBetting.Exceptions.csproj src/Shared/SportsBetting.Exceptions/
COPY tests/Validator.Tests/Validator.Tests.csproj tests/Validator.Tests/
COPY tests/CommonTestsUtilities/CommonTestsUtilities.csproj tests/CommonTestsUtilities/
COPY tests/UseCase.Test/UseCase.Test.csproj tests/UseCase.Test/
COPY tests/Integration.Test/Integration.Test.csproj tests/Integration.Test/
COPY tests/WebApi.Test/WebApi.Test.csproj tests/WebApi.Test/

RUN dotnet restore
COPY . .

RUN dotnet publish src/Backend/SportsBetting.API/SportsBetting.API.csproj -c Release -o /app/publish

# Migration stage: turns the versioned Entity Framework Core migrations into a
# standalone executable, so schema changes are an explicit deployment step and are
# never applied automatically when the API starts.
FROM build AS migrations-build
ENV PATH="$PATH:/root/.dotnet/tools"
# The design-time factory refuses to run without a connection string, and bundling never opens a
# connection, so a placeholder satisfies it here. The real connection string is passed to the
# bundle at run time via --connection (see docker-compose.yml).
ENV SPORTSBETTING_EF_CONNECTION="Server=design-time-placeholder;Database=SportsBetting;User Id=sa;Password=design-time-placeholder;TrustServerCertificate=True;"
RUN dotnet tool install --global dotnet-ef --version 10.0.*
RUN dotnet ef migrations bundle \
        --project src/Backend/SportsBetting.Infrastructure \
        --startup-project src/Backend/SportsBetting.Infrastructure \
        --context SportsBettingDbContext \
        --configuration Release \
        --force \
        -o /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# Runtime stage: the API itself, running as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

USER $APP_UID
ENTRYPOINT ["dotnet", "SportsBetting.API.dll"]
