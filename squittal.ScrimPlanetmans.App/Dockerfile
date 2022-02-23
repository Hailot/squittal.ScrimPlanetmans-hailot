#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app


FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["squittal.ScrimPlanetmans.App/squittal.ScrimPlanetmans.App.csproj", "squittal.ScrimPlanetmans.App/"]
RUN dotnet restore "squittal.ScrimPlanetmans.App/squittal.ScrimPlanetmans.App.csproj"
COPY . .
WORKDIR "/src/squittal.ScrimPlanetmans.App"
RUN dotnet build "squittal.ScrimPlanetmans.App.csproj" -c Release -o /app/build
EXPOSE 5000
WORKDIR "/src/squittal.ScrimPlanetmans.App"
