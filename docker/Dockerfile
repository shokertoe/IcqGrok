FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ICQ.Server.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/uploads
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ICQ.Server.dll"]
