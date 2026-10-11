# Build from the repository root:  docker build -f apps/api/Dockerfile .
# (docker compose does this.)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from only the files that decide what gets restored. This layer is cached
# until one of them changes, so editing a .cs file does not download the packages again.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY apps/api/src/SupportDesk.Domain/SupportDesk.Domain.csproj apps/api/src/SupportDesk.Domain/
COPY apps/api/src/SupportDesk.Application/SupportDesk.Application.csproj apps/api/src/SupportDesk.Application/
COPY apps/api/src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj apps/api/src/SupportDesk.Infrastructure/
COPY apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj apps/api/src/SupportDesk.Presentation/
RUN dotnet restore apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj

COPY apps/api/src apps/api/src
RUN dotnet publish apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj \
    --configuration Release --no-restore --output /app -p:UseAppHost=false

# The runtime image has no SDK and no source. No configuration or secret is baked in: the
# connection string, the JWT key and the environment name arrive as environment variables.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Not root. Plain HTTP on 8080; there are no certificates in the image.
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SupportDesk.Presentation.dll"]
