# BUILD STAGE
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY ["ManualShop.Api/ManualShop.Api.csproj", "ManualShop.Api/"]
COPY ["ManualShop.SharedKernel/ManualShop.SharedKernel.csproj", "ManualShop.SharedKernel/"]
# <modules>
COPY ["ManualShop.Products/ManualShop.Products.csproj", "ManualShop.Products/"]
COPY ["ManualShop.Orders/ManualShop.Orders.csproj", "ManualShop.Orders/"]
# </modules>
RUN dotnet restore "ManualShop.Api/ManualShop.Api.csproj" -a $TARGETARCH

COPY . .
WORKDIR "/src/ManualShop.Api"
RUN dotnet publish "ManualShop.Api.csproj" \
    -c Release \
    -o /app/publish \
    -a $TARGETARCH \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

# RUNTIME STAGE (Using Chiseled for Security & Size)
# No shell, no apt, no root. Perfect for k3s.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# k3s will handle the user context; Chiseled is non-root by default (UID 1654)
USER app

EXPOSE 8080

# OBSERVABILITY CONFIGURATION
# 1. Structured Logging for Loki
ENV Logging__Console__FormatterName=json
# 2. Globalization invariant mode (Saves space if you don't need culture-specific logic)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
# 3. Ensure OTel/Metrics are enabled
ENV OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_ENABLE_METRICS=true

ENTRYPOINT ["dotnet", "ManualShop.Api.dll"]