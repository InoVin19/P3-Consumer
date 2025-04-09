FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
# Expose base port - additional ports will be mapped in docker-compose.yml as needed
EXPOSE 9000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["P3-Consumer.csproj", "./"]
RUN dotnet restore "P3-Consumer.csproj"
COPY . .
RUN dotnet build "P3-Consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "P3-Consumer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY input.txt .
ENTRYPOINT ["dotnet", "P3-Consumer.dll"]