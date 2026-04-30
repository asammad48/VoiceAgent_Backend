# VoiceAgent Backend

## Local run

```bash
dotnet restore
dotnet build
dotnet run --project src/VoiceAgent.Api
```

## Docker

```bash
docker compose up --build
```

## Test endpoints

- GET http://localhost:8080/swagger
- GET http://localhost:8080/api/health
- GET http://localhost:8080/api/demo/campaigns

## Demo curl commands

```bash
curl -X POST http://localhost:8080/api/demo/start \
  -H "Content-Type: application/json" \
  -d '{}'

curl -X POST http://localhost:8080/api/demo/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What deals do you have?"
  }'

curl -X POST http://localhost:8080/api/demo/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I want two chicken burgers"
  }'

curl -X POST http://localhost:8080/api/demo/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is the menu?"
  }'

curl -X POST http://localhost:8080/api/demo/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I need courier from Manchester to Leeds, 3 kg parcel"
  }'
```
