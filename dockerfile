FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["Akasha.Consumer.csproj", "."]
RUN dotnet restore "Akasha.Consumer.csproj"  


COPY . .
RUN dotnet build "Akasha.Consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Akasha.Consumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app


COPY --from=publish /app/publish .

COPY --from=publish /app/publish/Migrations ./Migrations

ENTRYPOINT [ "dotnet", "Akasha.Consumer.dll" ]