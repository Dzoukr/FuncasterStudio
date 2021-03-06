﻿FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

# Install node & yarn
RUN curl -sL https://deb.nodesource.com/setup_14.x | bash
RUN apt-get update && apt-get install -y nodejs
RUN npm install -g yarn

WORKDIR /workspace
COPY . .
RUN dotnet tool restore

RUN dotnet run Publish


FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
COPY --from=build /workspace/publish/app /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT [ "dotnet", "FuncasterStudio.Server.dll" ]