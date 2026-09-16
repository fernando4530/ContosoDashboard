# Investigación: Document Upload and Management

## Decisiones

### Almacenamiento y flujo de persistencia

- **Decisión**: usar `IFileStorageService` con una implementación local configurable en
  `AppData/uploads`, fuera de `wwwroot`; persistir en la base únicamente una clave relativa
  opaca y metadatos. La clave se genera con GUID y una extensión derivada de la validación.
- **Racional**: evita path traversal, nombres duplicados y acoplamiento a filesystem o Azure.
  La separación permite reemplazar la implementación sin cambiar `DocumentService` ni las
  páginas.
- **Alternativas consideradas**: `wwwroot` con URL directa fue descartado porque omite
  autorización; guardar bytes en SQLite fue descartado por tamaño y por no respetar el límite
  de infraestructura local.

### Consistencia archivo/SQLite

- **Decisión**: flujo compensatorio por archivo: validar metadata y permisos, escribir temporal,
  escanear, promover al destino, guardar metadata y eliminar el temporal; ante fallo de EF,
  eliminar el destino y registrar el error. En reemplazos se conserva el archivo anterior hasta
  confirmar el nuevo metadata.
- **Racional**: filesystem y SQLite no comparten transacción. Cada archivo de una carga múltiple
  ejecuta el flujo de forma independiente para conservar éxitos parciales.
- **Alternativas consideradas**: insertar primero el registro fue descartado porque deja
  documentos accesibles sin archivo; `Task.WhenAll` sin aislamiento fue descartado porque
  complica compensación y presión de memoria.

### Validación y escaneo

- **Decisión**: límite exacto de 25 MiB, allowlist explícita de PDF, Office, TXT, JPEG y PNG,
  validación coherente de extensión/MIME y firma/contenido cuando corresponda, seguida de
  `IMalwareScanner`. Scanner no disponible equivale a rechazo.
- **Racional**: el nombre y MIME del navegador son señales no confiables; el requisito de
  seguridad exige que ningún archivo quede accesible sin análisis exitoso.
- **Alternativas consideradas**: confiar solo en extensión fue descartado por el caso de
  contenido engañoso; marcar como pendiente y mostrarlo fue descartado por la política de
  seguridad de la especificación.

### Autorización acumulativa

- **Decisión**: centralizar autorización en `DocumentAuthorizationService` y re-evaluarla para
  cada operación. Se permite si el usuario es propietario, administrador, miembro autorizado
  del proyecto, miembro del equipo asociado o destinatario explícito; estas fuentes se suman.
  Shares a equipo usan el `Department` existente como clave de equipo hasta que exista una
  entidad Team. Cambios de membresía/departamento y revocación de share se reflejan en la
  siguiente consulta.
- **Racional**: satisface la aclaración de acceso acumulativo y evita que una página o búsqueda
  implemente reglas distintas.
- **Alternativas consideradas**: copiar permisos al documento fue descartado porque no revoca
  accesos heredados; usar solo claims de rol fue descartado porque no conoce membresía actual.

### Descarga, preview y auditoría

- **Decisión**: endpoint autenticado que recibe solo `DocumentId`, consulta el documento mediante
  el servicio autorizado y abre la clave interna. Preview `inline` solo para PDF/JPEG/PNG; el
  resto usa descarga `attachment`. Descarga y las mutaciones generan `DocumentActivity`.
- **Racional**: elimina acceso directo a rutas y permite registrar el actor real y el momento.
- **Alternativas consideradas**: enlaces a archivos estáticos fueron descartados por IDOR y por
  imposibilidad de aplicar shares revocados.

## Riesgos y límites

- El escáner local necesita una política de configuración clara; la implementación de entrenamiento
  debe poder simular disponible, rechazado y no disponible para pruebas.
- Limpieza de temporales y archivos que sobrevivan a un crash debe quedar documentada y ser
  invocable al iniciar o desde una operación administrativa; no se promete durabilidad cloud.
- El modelo actual representa equipo por departamento, no por entidad independiente; esta es una
  decisión de alcance, no una garantía de producción.
