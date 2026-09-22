FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/EvidenceChain.Domain/*.csproj backend/EvidenceChain.Domain/
COPY backend/EvidenceChain.Application/*.csproj backend/EvidenceChain.Application/
COPY backend/EvidenceChain.Infrastructure/*.csproj backend/EvidenceChain.Infrastructure/
COPY backend/EvidenceChain.Api/*.csproj backend/EvidenceChain.Api/

RUN dotnet restore backend/EvidenceChain.Api/EvidenceChain.Api.csproj

COPY backend/ backend/

RUN dotnet publish backend/EvidenceChain.Api/EvidenceChain.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet EvidenceChain.Api.dll"]