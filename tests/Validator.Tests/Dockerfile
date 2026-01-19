FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/Validator.Tests/Validator.Tests.csproj", "tests/Validator.Tests/"]
COPY ["tests/CommonTestsUtilities/CommonTestsUtilities.csproj", "tests/CommonTestsUtilities/"]
COPY ["src/Backend/SportsBetting.Infrastructure/SportsBetting.Infrastructure.csproj", "src/Backend/SportsBetting.Infrastructure/"]
COPY ["src/Backend/SportsBetting.Application/SportsBetting.Application.csproj", "src/Backend/SportsBetting.Application/"]
COPY ["src/Shared/SportsBetting.Communication/SportsBetting.Communication.csproj", "src/Shared/SportsBetting.Communication/"]
COPY ["src/Shared/SportsBetting.Exceptions/SportsBetting.Exceptions.csproj", "src/Shared/SportsBetting.Exceptions/"]
COPY ["src/Backend/SportsBetting.Domain/SportsBetting.Domain.csproj", "src/Backend/SportsBetting.Domain/"]
RUN dotnet restore "tests/Validator.Tests/Validator.Tests.csproj"
COPY . .
WORKDIR "/src/tests/Validator.Tests"
RUN dotnet build "./Validator.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Validator.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Validator.Tests.dll"]
