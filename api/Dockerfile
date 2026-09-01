FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY RunClub.sln ./
COPY api/RunClub.Domain/RunClub.Domain.csproj api/RunClub.Domain/
COPY api/RunClub.Application/RunClub.Application.csproj api/RunClub.Application/
COPY api/RunClub.Infrastructure/RunClub.Infrastructure.csproj api/RunClub.Infrastructure/
COPY api/RunClub.Api/RunClub.Api.csproj api/RunClub.Api/
RUN dotnet restore api/RunClub.Api/RunClub.Api.csproj
COPY api/ api/
RUN dotnet publish api/RunClub.Api/RunClub.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RunClub.Api.dll"]
