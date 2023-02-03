FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

ARG TARGETPLATFORM

COPY Archiver/*.csproj .
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ] || [ "$TARGETPLATFORM" = "linux/arm/v8" ]; then \
        RID=linux-arm ; \
    fi \
    && echo "dotnet restore -runtime $RID" \
    && dotnet restore -runtime $RID

COPY . .
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ] || [ "$TARGETPLATFORM" = "linux/arm/v8" ]; then \
        RID=linux-arm ; \
    fi \
    && echo "dotnet publish -c Release -o /app --runtime $RID --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false" \
    && dotnet publish -c Release -o /app --runtime $RID --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:3.1-buster-slim
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Archiver.dll"]
