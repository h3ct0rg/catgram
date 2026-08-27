# Kindred Paws — Plan de implementación

## 1. Objetivo y alcance

Construir una red social pública enfocada en animales de refugios. Los refugios y usuarios autorizados podrán administrar refugios, animales, publicaciones e historias; cualquier visitante podrá descubrir contenido, consultar perfiles y compartir enlaces para aumentar las oportunidades de adopción.

El producto debe conservar la relación de dominio:

> Refugio → Animal → Publicaciones / Historias → Interacciones

El frontend seguirá los diseños existentes de `design/`: experiencia mobile-first, feed visual tipo Instagram, historias de 24 horas, ficha permanente del animal y formulario de registro. La dirección visual será glassmorphism sobre azul sobrio y juvenil, usando Plus Jakarta Sans.

## 2. Decisiones técnicas

| Área | Decisión | Motivo |
|---|---|---|
| Frontend | React + TypeScript + Vite | Desarrollo rápido, tipado y buena experiencia para una SPA responsive |
| UI | Tailwind CSS + tokens del diseño + CSS Modules cuando sea necesario | Permite reproducir el sistema visual sin duplicar estilos |
| Backend | ASP.NET Core .NET 10 Web API | APIs tipadas, autenticación integrada y buen rendimiento |
| Arquitectura backend | Modular monolith por módulos de dominio | Menor complejidad inicial y camino claro para escalar |
| BD principal | PostgreSQL 17 | Relacional, consistente y excelente para filtros, relaciones, auditoría y búsqueda inicial |
| Acceso a datos | Entity Framework Core + consultas proyectadas | Productividad, migraciones y evitar traer datos innecesarios |
| Cache | Redis (fase de rendimiento) | Feed, sesiones auxiliares, rate limit y datos de lectura frecuente |
| Archivos | MinIO vía S3-compatible API | Fotos/videos fuera de la BD, URLs firmadas y escalabilidad |
| Mensajería | RabbitMQ | Notificaciones y tareas asíncronas desacopladas |
| Autenticación | ASP.NET Core Identity + Google OAuth para invitados; credencial local exclusiva para Super Admin | Cumple los dos caminos solicitados y mantiene roles centralizados |
| Contrato API | OpenAPI/Swagger + DTOs versionados | Facilita el consumo desde React y pruebas de contrato |

### Nota importante sobre configuración

El archivo recibido se llama `appsettincs.json` y contiene credenciales de MinIO. Debe normalizarse a `appsettings.json` en el backend, pero los secretos no deben quedar en el repositorio. Para desarrollo se pueden usar User Secrets o variables de entorno; en producción, un gestor de secretos. El seed solicitado para `superadmin / superadmin` debe existir únicamente como credencial inicial de desarrollo/demo y obligar al cambio de contraseña en el primer acceso.

## 3. Roles y autenticación

### Roles iniciales

- **Visitante:** navegación pública, feed, historias, perfiles y compartir.
- **Usuario:** visitante + likes, comentarios, respuestas y seguimiento de animales.
- **Administrador:** gestión de refugios, animales, publicaciones, historias y moderación según permisos.
- **Super Administrador:** usuarios, invitaciones, roles, configuración global y auditoría.

### Flujos

1. El Super Administrador inicia sesión con usuario y contraseña local.
2. El Super Administrador invita a una persona indicando nombre, correo, rol y, si aplica, refugio.
3. La persona usa el enlace de invitación, inicia el flujo OAuth de Google y se vincula el `GoogleSubject` con la invitación pendiente.
4. Solo una invitación vigente y no utilizada permite crear/activar la cuenta.
5. Las invitaciones expiran, se almacenan con token hash y no exponen el token en logs.
6. Todas las APIs privadas validan JWT/cookie seguro y autorización por política/rol.

El requisito de recuperación de contraseña del resumen debe tratarse como **fuera del flujo normal para usuarios Google**; para el Super Admin se recomienda recovery administrativo y rotación obligatoria de la credencial inicial.

## 4. Fases de implementación

> Convención: marcar `[x]` cuando la fase cumpla sus criterios de salida. Mantener la casilla en `[ ]` mientras esté pendiente.

### Fase 0 — Preparación y decisiones cerradas

**Estado:** [ ] Implementada técnicamente; pendiente de validación del usuario
**Objetivo:** dejar lista la base técnica y confirmar los contratos que afectan a todo el producto.

**Entregables**

- [x] Crear solución .NET 10 y aplicación React + TypeScript.
- [x] Configurar repositorio, convenciones, editorconfig, linting, formateo y CI básico.
- [x] Normalizar `appsettincs.json` a configuración .NET por ambientes.
- [x] Definir OpenAPI, manejo de errores con `ProblemDetails`, logs estructurados y correlation ID.
- [x] Convertir los tokens de `design/kindred_paws/DESIGN.md` en variables reutilizables del frontend.
- [x] Decidir dominios, CORS, URLs de Google OAuth, MinIO y RabbitMQ por ambiente.

**Criterios de salida**

- Frontend y backend arrancan localmente con un README reproducible.
- No hay secretos reales en archivos versionados.
- Existe un pipeline que compila, ejecuta pruebas y valida formato.

### Fase 1 — Identidad, invitaciones y RBAC

**Estado:** [ ] Implementada técnicamente; pendiente de validación del usuario  
**Historias:** US-001 a US-007.

**Entregables**

- [x] ASP.NET Core Identity con usuario, roles y estado activo/bloqueado.
- [x] Seed de roles y Super Admin inicial `superadmin / superadmin` para desarrollo.
- [x] Forzar cambio de contraseña en primer acceso y documentar rotación inmediata.
- [x] Login local del Super Admin y emisión de JWT.
- [x] Google OAuth restringido a invitaciones válidas para el resto de usuarios.
- [x] Gestión de invitaciones: emitir, consultar, revocar, expirar y consumir una sola vez.
- [x] Pantalla React base de login, integración del login local y entrada al flujo Google por invitación.
- [x] Pantallas de aceptación de invitación, sesión expirada y acceso denegado.
- [x] Guardas de autorización por rol en API y matriz de permisos documentada.

**Nota de mensajería:** las notificaciones no se envían desde la API. La API publica eventos en RabbitMQ y `backend/worker/KindredPaws.NotificationWorker` los consume para enviar correos. La integración productiva de plantillas, SMTP administrado, reintentos con dead-letter queue y observabilidad queda en la fase de infraestructura/notificaciones.

**Criterios de salida**

- Un visitante puede consumir la parte pública sin autenticarse.
- El Super Admin puede invitar y administrar usuarios.
- Un usuario invitado no puede acceder a administración sin el rol correcto.

### Fase 2 — Refugios y animales

**Estado:** [x] Implementada técnicamente; pendiente de validación del usuario
**Historias:** US-010 a US-014 y US-090 a US-093.

**Entregables**

- [x] Entidades y endpoints de refugio, animal, galería y ubicación.
- [x] Estados: Disponible, En proceso, Adoptado, No disponible y Fallecido.
- [x] Alta/edición de animal con validación, foto principal y galería.
- [x] Carga de archivos a MinIO con validación de MIME y tamaño; antivirus queda pendiente de infraestructura.
- [x] Perfil público del animal mediante endpoint preparado para la ficha de Rocky.
- [x] Perfil público del refugio con animales.
- [x] Generación automática de thumbnails (ImageSharp, solo imágenes; video queda pendiente de una librería de extracción de frames).
- [x] URLs firmadas de MinIO para los medios.

**Criterios de salida**

- Un administrador puede registrar a Rocky o cualquier animal sin guardar binarios en PostgreSQL.
- El perfil público funciona desde enlace directo y es responsive.
- Cambiar el estado de adopción queda auditado.

### Fase 3 — Feed público, publicaciones e historias

**Estado:** [x] Implementada técnicamente; pendiente de validación del usuario
**Historias:** US-020 a US-034 y US-070 a US-074.

**Entregables**

- [x] Crear, editar, ocultar y destacar publicaciones.
- [x] Asociar publicación a animal, refugio, ubicación, hashtags y galería multimedia.
- [x] Feed público con paginación por cursor (recientes) y por offset (populares).
- [x] Historias con expiración a 24 horas, contador de vistas y asociación a animal.
- [x] Endpoints públicos y administrativos preparados para el feed y stories, incluyendo `GET /api/v1/social/posts/{id}` para publicación individual.
- [x] Infinite scroll real integrado en el frontend, contra el feed paginado del backend (ya no es una simulación local).
- [x] Ordenamiento real: recientes (por fecha) y populares (por cantidad de likes).
- [x] Open Graph (`GET /p/{id}`) para que los enlaces compartidos muestren imagen, nombre del animal, refugio, descripción y URL; redirige al humano hacia la SPA.
- [x] Reproducir la base visual del feed: header, stories, tarjetas, estados, navegación inferior y glassmorphism. Incluye ahora un visor de historias real (pantalla completa, navegación, `/stories/:id`).

**Arquitectura frontend:** la interfaz quedó separada por features y componentes reutilizables; `main.tsx` monta la aplicación envuelta en `SessionProvider`; `app/App.tsx` define las rutas con `react-router-dom`; `context/SessionContext.tsx` centraliza sesión/rol derivados del JWT; `services/apiClient.ts` centraliza HTTP (con inyección de `Authorization` y manejo de 401) y `features/`/`components/` contienen la UI de cada dominio.

**Criterios de salida**

- El feed público funciona sin login y mantiene buen rendimiento con paginación.
- Un administrador puede publicar una actualización con imagen/video.
- Una historia expirada no aparece en consultas públicas.

### Fase 4 — Engagement y notificaciones

**Estado:** [x] Implementada técnicamente; pendiente de validación del usuario y de pruebas contra infraestructura real (Postgres/RabbitMQ/MinIO)
**Historias:** US-040 a US-074, US-120 a US-132.

**Entregables**

- [x] Likes idempotentes (índice único `PostId+UserId`) y contadores consistentes, calculados en el backend.
- [x] Comentarios, respuestas (un nivel de anidamiento), eliminación propia y moderación básica (ocultar por Admin/SuperAdmin).
- [x] Reporte de contenido (publicación/comentario) y usuario desde UI autenticada (solo creación; la bandeja de revisión es Fase 5).
- [x] Compartir por Web Share API con fallback a WhatsApp/Facebook/X y copiar enlace.
- [x] Seguir/dejar de seguir animales, con botón en el perfil del animal.
- [x] Eventos de dominio en RabbitMQ: like, comentario, respuesta, cambio de estado de adopción y nueva publicación (fan-out por seguidor).
- [x] Worker de notificaciones con dead-letter queue nativa de RabbitMQ, idempotencia vía tabla propia (`worker_processed_events`) y despacho por tipo de evento.
- [x] Centro de notificaciones in-app (creadas sincrónicamente por la API) con preferencias básicas por tipo, campana con contador de no leídas (polling) en el frontend.

**Decisiones técnicas de esta fase:**
- Las notificaciones in-app las crea la API en el mismo request que la acción (no dependen de RabbitMQ/worker); RabbitMQ + worker quedan exclusivamente para el envío de email, preservando el criterio de que una caída del consumidor no bloquee la acción principal.
- Se introdujeron migraciones EF Core reales (`Database.MigrateAsync`), reemplazando `EnsureCreatedAsync`, para soportar de forma versionada las tablas nuevas de esta fase.
- El feed (`GET /api/v1/social/feed`) ahora devuelve además nombre de refugio/animal, estado de adopción, contadores de like/comentario y si el usuario actual dio like, para que el frontend deje de mostrar datos simulados.
- El frontend migró de un router hash artesanal a `react-router-dom`, habilitando URLs por publicación (`/p/:id`), por animal (`/animals/:id`), por historia (`/stories/:id`) y de notificaciones (`/notifications`).

**Criterios de salida**

- Las acciones repetidas no duplican likes ni notificaciones (verificado a nivel de lógica/índices; falta prueba end-to-end contra una instancia real).
- La caída temporal del consumidor RabbitMQ no bloquea la publicación principal (las notificaciones in-app y la acción del usuario no dependen del worker).
- Un usuario recibe novedades de animales que sigue (evento + notificación in-app por seguidor).

**Pendiente de verificación (no ejecutable en este entorno de trabajo):** no hay Postgres/RabbitMQ/MinIO corriendo localmente ni `docker-compose` en el repo, por lo que esta fase se validó a nivel de compilación (`dotnet build`, migraciones generadas con `dotnet ef migrations add`, `tsc -b`, `vite build`), no con una corrida real de extremo a extremo. Falta además: probar el ordenamiento "populares" con datos reales, y una revisión visual de las pantallas nuevas contra `design/` con el equipo de producto (no existen mockups para comentarios, compartir, reportar, notificaciones ni el visor de historias, así que se construyeron extendiendo los tokens existentes).

### Fase 5 — Moderación, auditoría y administración

**Estado:** [ ] No implementada  
**Historias:** US-100 a US-111.

**Entregables**

- [ ] Bandeja de reportes con filtros por estado y tipo.
- [ ] Ocultar/eliminar contenido y bloquear usuarios.
- [ ] Auditoría de creación, modificación, eliminación, cambios de rol y estados de adopción.
- [ ] Dashboard con usuarios, refugios, animales, publicaciones, historias e interacciones.
- [ ] Estadísticas por animal: vistas, likes, comentarios, compartidos y adopción.
- [ ] Políticas de retención para logs y auditoría.

**Criterios de salida**

- Un moderador puede resolver un reporte con trazabilidad.
- El Super Admin puede identificar quién hizo una modificación sensible.
- Las métricas del dashboard coinciden con consultas verificables.

### Fase 6 — Descubrimiento y adopción

**Estado:** [ ] No implementada  
**Historias:** US-080 a US-083 y US-140 a US-151.

**Entregables**

- [ ] Búsqueda por nombre de animal/refugio y filtros por especie, sexo, edad, tamaño, raza, ubicación y estado.
- [ ] Índices PostgreSQL y, si el volumen lo justifica, búsqueda dedicada.
- [ ] “Animales cerca de mí” con geolocalización opcional y consentimiento.
- [ ] Solicitud de adopción, formulario configurable y estados de revisión.
- [ ] Registro de adopción y cambio automático a Adoptado.
- [ ] Historias de éxito “final feliz”.

**Criterios de salida**

- Un visitante encuentra animales disponibles con filtros combinables.
- Un refugio puede revisar una solicitud de adopción sin modificar datos directamente.
- Una adopción queda vinculada al animal y se refleja en el feed/historia.

### Fase 7 — Calidad, rendimiento y operación

**Estado:** [ ] No implementada  
**Objetivo:** preparar la primera versión productiva.

**Entregables**

- [ ] Pruebas unitarias de dominio, integración de API y pruebas E2E de flujos críticos.
- [ ] Pruebas de carga para feed, búsqueda, subida de archivos y consumo de notificaciones.
- [ ] Redis para cache de consultas públicas y rate limiting.
- [ ] Compresión, thumbnails, lazy loading y CDN/proxy para medios.
- [ ] Health checks de PostgreSQL, Redis, MinIO y RabbitMQ.
- [ ] Métricas, trazas, alertas y backups/restauración de PostgreSQL y MinIO.
- [ ] Revisión de accesibilidad WCAG, seguridad, CORS, CSP, CSRF/cookies y límites de subida.
- [ ] Documentación de despliegue, rollback y recuperación ante incidentes.

**Criterios de salida**

- Los flujos críticos están automatizados y medidos.
- Existe un procedimiento probado de restauración.
- El sistema tiene límites de rendimiento y seguridad aceptados por el equipo.

## 5. Backlog MVP recomendado

La primera versión publicable debe incluir las fases 0 a 4 recortadas a lo esencial:

1. Identidad, invitaciones, roles y Super Admin.
2. Refugios, animales, estados y galerías.
3. Feed público, publicaciones e historias.
4. Likes, comentarios, compartir, reportes y notificaciones básicas.

La búsqueda avanzada, adopciones formales, historias de éxito y analítica profunda pueden entrar inmediatamente después del MVP sin romper el modelo de dominio.

## 6. Reglas de UI derivadas de los diseños

- Mobile-first: 16 px de margen móvil, 32 px en escritorio y ritmo de 8 px.
- Feed centrado aproximadamente en 600 px; dashboards con grid de 12 columnas.
- Fondo con gradientes radiales `#E0F2F7` → `#FFFFFF`.
- Tarjetas de vidrio con `rgba(255,255,255,.7)`, borde blanco translúcido, blur de 12 px y sombra azul difusa.
- Primario sobrio `#0059BB`; acento juvenil `#2E5BFF`; texto principal `#181C23`.
- Plus Jakarta Sans: títulos 700/800, cuerpo 400/500, labels 600/700.
- Estados de adopción deben usar colores sólidos y accesibles: verde, ámbar, azul, rojo y negro.
- Barra de navegación inferior fija en móvil, respetando safe areas.
- Estados de carga, vacío, error, imagen rota y sesión expirada deben diseñarse desde el primer sprint.
- Mantener nombres de dominio en español en API/base de datos y textos de interfaz internacionalizables.

## 7. Riesgos y controles

| Riesgo | Control |
|---|---|
| Secretos expuestos en `appsettincs.json` | Variables de entorno/User Secrets, rotación de credenciales y `.gitignore` |
| Feed lento por imágenes pesadas | Thumbnails, lazy loading, URLs firmadas, CDN y paginación por cursor |
| Duplicados por reintentos | Idempotency keys, claves únicas y consumidores RabbitMQ idempotentes |
| Publicación pública de contenido inapropiado | Reportes, moderación, bloqueo, auditoría y límites de frecuencia |
| OAuth usado sin invitación | Validar invitación antes de activar la cuenta y vincular el subject de Google |
| Seed de contraseña inseguro | Solo desarrollo/demo, flag de primer acceso y rotación obligatoria |
| Pérdida de archivos | Versionado/replicación de MinIO, backups y prueba de restauración |

## 8. Definition of Done transversal

- [ ] Criterios de aceptación cubiertos.
- [ ] Validación frontend y backend.
- [ ] Autorización probada para cada rol.
- [ ] Auditoría aplicada a cambios sensibles.
- [ ] Pruebas automatizadas relevantes.
- [ ] Estados de carga, vacío y error implementados.
- [ ] Diseño responsive validado contra los PNG de `design/`.
- [ ] Documentación y variables de configuración actualizadas.
- [ ] Sin secretos, tokens ni archivos multimedia de prueba sensibles en el repositorio.
