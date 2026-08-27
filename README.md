# Kindred Paws

Base inicial para la red social pública de animales de refugios.

## Estructura

- `frontend/`: React + TypeScript + Vite.
- `backend/KindredPaws.slnx`: solución del backend.
- `backend/api/KindredPaws.Api/`: ASP.NET Core Web API sobre .NET 10.
- `backend/worker/KindredPaws.NotificationWorker/`: consumidor RabbitMQ y envío de correos.
- `backend/shared/KindredPaws.Contracts/`: contratos de eventos compartidos.
- `docs/`: decisiones de arquitectura.
- `design/`: referencias visuales entregadas.

## Arranque local

### API

```powershell
dotnet run --project backend/api/KindredPaws.Api --launch-profile http
```

- API: `http://localhost:5080`
- Health check: `http://localhost:5080/health`
- OpenAPI (Development): `http://localhost:5080/openapi/v1.json`

### Notification worker

```powershell
dotnet run --project backend/worker/KindredPaws.NotificationWorker
```

La API publica eventos en la cola `kindred-paws.notifications`; el worker los consume y envía el correo mediante SMTP.

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

El frontend queda en `http://localhost:5173`.

Para revisar o aplicar el formato del frontend:

```powershell
npm run lint
npm run format
```

La configuración sensible debe cargarse desde variables de entorno/User Secrets usando `.env.example` y `appsettings.example.json` como referencia.
