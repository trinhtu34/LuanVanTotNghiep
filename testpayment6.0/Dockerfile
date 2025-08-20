#default dockerfile
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["testpayment6.0/testpayment6.0.csproj", "testpayment6.0/"]
#RUN dotnet restore "./testpayment6.0/testpayment6.0.csproj"
#COPY . .
#WORKDIR "/src/testpayment6.0"
#RUN dotnet build "./testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "testpayment6.0.dll"]





# Base image để chạy mà không cần phải bật visual studio 
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy file csproj và restore
COPY ["testpayment6.0.csproj", "."]
RUN dotnet restore "testpayment6.0.csproj"

# Copy toàn bộ source code
COPY . .

# Build project
RUN dotnet build "testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "testpayment6.0.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "testpayment6.0.dll"]

# dựa vào cái này để làm CI/CD cho frontend ha 
#docker build -t testpayment60:manual .
#docker run -d -p 32770:80 --name testpayment60-container testpayment60:manual

#docker tag testpayment60:manual 730335485777.dkr.ecr.ap-southeast-1.amazonaws.com/test-codepipline-and-step-functions:latest