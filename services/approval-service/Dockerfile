FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY src/Lehrerleicht.Approval.Core/Lehrerleicht.Approval.Core.csproj src/Lehrerleicht.Approval.Core/
COPY src/Lehrerleicht.Approval.Infrastructure/Lehrerleicht.Approval.Infrastructure.csproj src/Lehrerleicht.Approval.Infrastructure/
COPY src/Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj src/Lehrerleicht.Approval.Api/
RUN dotnet restore src/Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj

COPY src/ src/
RUN dotnet publish src/Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lehrerleicht.Approval.Api.dll"]
