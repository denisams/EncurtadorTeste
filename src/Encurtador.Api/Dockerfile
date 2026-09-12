FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY EncurtadorTeste.slnx ./
COPY src/Encurtador.Api/Encurtador.Api.csproj src/Encurtador.Api/
COPY src/Encurtador.Domain/Encurtador.Domain.csproj src/Encurtador.Domain/
COPY src/Encurtador.Application/Encurtador.Application.csproj src/Encurtador.Application/
COPY src/Encurtador.Infrastructure/Encurtador.Infrastructure.csproj src/Encurtador.Infrastructure/
COPY tests/Encurtador.Tests/Encurtador.Tests.csproj tests/Encurtador.Tests/
RUN dotnet restore src/Encurtador.Api/Encurtador.Api.csproj

COPY . .
RUN dotnet publish src/Encurtador.Api/Encurtador.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Encurtador.Api.dll"]
