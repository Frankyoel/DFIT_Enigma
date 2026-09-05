FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Dfit.csproj", "./"]
RUN dotnet restore "Dfit.csproj"
COPY . .
RUN dotnet publish "Dfit.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Copiamos la llave de firebase al contenedor
COPY firebase-key.json .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
ENTRYPOINT ["dotnet", "Dfit.dll"]
