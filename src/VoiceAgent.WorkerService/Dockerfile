# WorkerService Dockerfile scaffold
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Copy publish output in implementation stage
# COPY ./publish .

ENTRYPOINT ["dotnet", "VoiceAgent.WorkerService.dll"]
