FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GoodReceivedAPI.csproj", "./"]
RUN dotnet restore "./GoodReceivedAPI.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "GoodReceivedAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GoodReceivedAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GoodReceivedAPI.dll"]