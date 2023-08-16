FROM mcr.microsoft.com/dotnet/sdk:6.0 AS installer-env

COPY --from=mcr.microsoft.com/dotnet/sdk:7.0.203-bullseye-slim /usr/share/dotnet /usr/share/dotnet

WORKDIR /src
COPY ["Arcus.WebApi.Tests.Runtimes.AzureFunction/Arcus.WebApi.Tests.Runtimes.AzureFunction.csproj", "Arcus.WebApi.Tests.Runtimes.AzureFunction/"]
COPY ["Arcus.WebApi.Logging.AzureFunctions/Arcus.WebApi.Logging.AzureFunctions.csproj", "Arcus.WebApi.Logging.AzureFunctions/"]
COPY ["Arcus.WebApi.Logging.Core/Arcus.WebApi.Logging.Core.csproj", "Arcus.WebApi.Logging.Core/"]

RUN dotnet restore "Arcus.WebApi.Tests.Runtimes.AzureFunction/Arcus.WebApi.Tests.Runtimes.AzureFunction.csproj"
COPY . .
WORKDIR "/src/Arcus.WebApi.Tests.Runtimes.AzureFunction"

RUN dotnet publish "Arcus.WebApi.Tests.Runtimes.AzureFunction.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet:4
ENV AzureWebJobsScriptRoot=/app/publish \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=installer-env [ "/app/publish", "/app/publish" ]