FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

COPY Archiver/*.csproj .
RUN dotnet restore --runtime linux-x64

COPY . .
RUN dotnet publish -c Release -o /app --runtime linux-x64 --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/runtime:3.1-buster-slim
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Archiver.dll"]
