# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.


#default dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["testpayment6.0/testpayment6.0.csproj", "testpayment6.0/"]
RUN dotnet restore "./testpayment6.0/testpayment6.0.csproj"
COPY . .
WORKDIR "/src/testpayment6.0"
RUN dotnet build "./testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "testpayment6.0.dll"]









## Base image để chạy app
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#WORKDIR /app
#EXPOSE 8080
#
## Build image
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#
## Copy file csproj và restore
#COPY ["testpayment6.0.csproj", "."]
#RUN dotnet restore "testpayment6.0.csproj"
#
## Copy toàn bộ source code
#COPY . .
#
## Build project
#RUN dotnet build "testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
## Publish project
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
## Final image
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "testpayment6.0.dll"]


#docker build -t testpayment60:manual .
#docker run -d -p 32770:8080 --name testpayment60-container testpayment60:manual


