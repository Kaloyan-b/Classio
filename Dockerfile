FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Classio/Classio.csproj Classio/
COPY Classio.Tests/Classio.Tests.csproj Classio.Tests/
COPY Classio.sln .
RUN dotnet restore

COPY . .
RUN dotnet publish Classio/Classio.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Classio.dll"]
