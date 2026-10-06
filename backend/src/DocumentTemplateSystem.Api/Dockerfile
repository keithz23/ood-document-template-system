FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY backend/ ./backend/
RUN dotnet restore backend/src/DocumentTemplateSystem.Api/DocumentTemplateSystem.Api.csproj \
    --arch $TARGETARCH
RUN dotnet publish backend/src/DocumentTemplateSystem.Api/DocumentTemplateSystem.Api.csproj \
    --configuration Release \
    --arch $TARGETARCH \
    --no-restore \
    --output /app/publish

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "DocumentTemplateSystem.Api.dll"]
