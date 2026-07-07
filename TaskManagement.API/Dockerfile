# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition files and restore dependencies
COPY ["TaskManagement.slnx", "./"]
COPY ["TaskManagement.API/TaskManagement.API.csproj", "TaskManagement.API/"]
COPY ["TaskManagement.Application/TaskManagement.Application.csproj", "TaskManagement.Application/"]
COPY ["TaskManagement.Domain/TaskManagement.Domain.csproj", "TaskManagement.Domain/"]
COPY ["TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj", "TaskManagement.Infrastructure/"]
COPY ["TaskManagement.Domain.Tests/TaskManagement.Domain.Tests.csproj", "TaskManagement.Domain.Tests/"]
COPY ["TaskManagement.API.IntegrationTests/TaskManagement.API.IntegrationTests.csproj", "TaskManagement.API.IntegrationTests/"]

RUN dotnet restore

# Copy all source files
COPY . .
WORKDIR "/src/TaskManagement.API"
RUN dotnet publish "TaskManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TaskManagement.API.dll"]
