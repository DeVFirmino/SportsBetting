FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solution e todos os csproj
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
COPY tests/WebApi.Test/WebApi.Test.csproj tests/WebApi.Test/

RUN dotnet restore
COPY . .

# Build apenas o projeto API
WORKDIR /src/src/Backend/SportsBetting.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SportsBetting.API.dll"]
