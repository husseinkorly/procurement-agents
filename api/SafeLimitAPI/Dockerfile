FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SafeLimitAPI.csproj", "."]
RUN dotnet restore "./SafeLimitAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SafeLimitAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SafeLimitAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SafeLimitAPI.dll"]