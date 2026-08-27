# Kindred Paws — Arquitectura técnica de referencia

## Componentes

```text
React SPA
   │ HTTPS / OpenAPI
ASP.NET Core .NET 10 API
   ├── Identity/Auth + Google OAuth
   ├── Shelters & Animals
   ├── Posts & Stories
   ├── Engagement & Moderation
   ├── Adoption (posterior al MVP)
   └── Notifications publisher ───────┐
        │                             │
        ▼                             ▼
   PostgreSQL                 RabbitMQ → Notification Worker → SMTP/Mail provider
        │
      Redis (cache/rate limit, posterior)

React/API ── S3 API ── MinIO (media y thumbnails)
```

## Módulos y entidades principales

- `Identity`: User, Role, Invitation, ExternalLogin, RefreshSession.
- `Shelters`: Shelter, ShelterMember, Address.
- `Animals`: Animal, AnimalMedia, AnimalStatusHistory.
- `Social`: Post, PostMedia, Story, Hashtag, Like, Comment, CommentReport, AnimalFollow.
- `Moderation`: ContentReport, UserBlock, AuditEntry.
- `Notifications`: Notification, NotificationPreference, OutboxMessage.
- `Adoption`: AdoptionForm, AdoptionApplication, AdoptionStatusHistory, AdoptionRecord.

Usar `UUID` como identificador público, timestamps UTC, concurrencia optimista y borrado lógico para contenido moderable. Las relaciones críticas deben tener índices y restricciones únicas.

## Patrones de rendimiento

- Feed por cursor `(CreatedAt, Id)`; evitar paginación por offset para contenido de alto volumen.
- DTOs proyectados, `AsNoTracking()` en consultas públicas y límites de página.
- Contadores denormalizados solo donde el volumen lo justifique, con reconciliación periódica.
- Outbox transaccional para publicar eventos en RabbitMQ sin perder mensajes después de confirmar la BD.
- MinIO para originales y derivados; PostgreSQL almacena metadata, ownership y claves de objetos.
- Redis se incorpora después de medir, para evitar cachear invalidaciones innecesarias.

## Seguridad mínima

- HTTPS, cookies `HttpOnly/Secure/SameSite` o JWT con refresh rotation.
- Validación de MIME real y tamaño de archivos; nunca confiar solo en la extensión.
- Rate limiting para login, invitaciones, comentarios, reportes y subida de medios.
- CORS por lista blanca; CSP sin fuentes arbitrarias en producción.
- No registrar contraseñas, tokens OAuth, secretos MinIO ni tokens de invitación.
- El Super Admin inicial se marca `MustChangePassword = true`.

## Contratos de configuración

La configuración sugerida está en `.env.example`. En .NET se recomienda mapearla mediante `__` a secciones como `ConnectionStrings`, `Minio`, `RabbitMq` y `Authentication:Google`.

## Estructura de la API

```text
backend/api/KindredPaws.Api/
├── Controllers/              # HTTP: request/response y autorización
├── Application/              # Casos de uso y reglas de aplicación
│   ├── Auth/                 # Login, OAuth, invitaciones
│   ├── Users/                # Administración de usuarios
│   └── Shared/               # Contratos de eventos y correo
├── Domain/                   # Entidades, constantes y reglas puras
└── Infrastructure/          # EF Core, repositorios y adaptadores externos

backend/worker/KindredPaws.NotificationWorker/
└── WorkerServices.cs         # Consumidor RabbitMQ y envío de correo

frontend/src/
├── app/                       # Composición y navegación de la aplicación
├── components/                # Componentes compartidos de layout
├── features/                  # UI y casos de uso por dominio
│   ├── animals/
│   ├── auth/
│   └── feed/
├── services/                  # Cliente HTTP y adaptadores frontend
├── types/                     # Contratos TypeScript
└── styles.css                 # Tokens y estilos globales
```

Regla de dependencia: un controller no consulta `DbContext`, no publica eventos y no envía correos. La API publica eventos en RabbitMQ desde servicios de aplicación; el worker los consume y llama al proveedor SMTP mediante otra abstracción. Así una caída del correo no bloquea el request HTTP y se puede reintentar la entrega desde la cola.

El feed se consulta por cursor usando `CreatedAt` y límite de página. Las publicaciones públicas excluyen contenido oculto/eliminado; las historias se filtran por `ExpiresAt` en UTC. Los objetos multimedia se guardan en MinIO y las respuestas exponen URLs firmadas, nunca binarios almacenados en PostgreSQL.
