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
   └── Notifications publisher
        │                         │
        ▼                         ▼
   PostgreSQL                 RabbitMQ → Notification Worker
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
