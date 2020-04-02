FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .

COPY Server/*.csproj ./Server/
RUN dotnet restore Server

# copy everything else and build app

COPY Server ./Server/
WORKDIR /app/Server
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

WORKDIR /app

COPY --from=build /app/Server/out ./

EXPOSE 80

ENTRYPOINT ["dotnet", "Server.dll"]