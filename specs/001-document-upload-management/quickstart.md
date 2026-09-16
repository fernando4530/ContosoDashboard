# Guía de validación

## Prerrequisitos

- .NET SDK 10.0 y navegador local.
- Ejecutar desde la raíz: `dotnet restore`.
- El usuario de entrenamiento se selecciona en `/login`; la base SQLite se crea con el flujo
  actual de `EnsureCreated`.
- Configurar la carpeta local en `appsettings.json` o `appsettings.Development.json`; debe estar
  fuera de `ContosoDashboard/wwwroot`.

## Validación automatizada

Ejecutar `dotnet test` para cubrir validación, almacenamiento falso, autorización, shares
revocados, membresía removida, auditoría, compensación y consultas SQLite. Ejecutar además
`dotnet build` y verificar que no aparezcan advertencias nuevas.

## Escenarios manuales

1. Iniciar sesión como empleado, cargar un PDF válido con título/categoría y verificar progreso,
   lista personal, tamaño, tipo y archivo fuera de `wwwroot`.
2. En una carga múltiple mezclar un archivo válido, uno de más de 25 MiB y uno con MIME/contenido
   inválido. Confirmar que el válido queda disponible y cada rechazo conserva su motivo.
3. Probar scanner rechazado y scanner no disponible. Confirmar que ningún documento aparece en
   búsquedas o listas.
4. Como miembro del proyecto, abrir sus documentos, previsualizar PDF/imagen y descargar. Como
   usuario no miembro, intentar el mismo `DocumentId` y confirmar respuesta denegada sin revelar
   la ruta.
5. Como propietario, compartir con otro usuario y con el departamento del equipo; verificar
   `Shared with Me` y notificaciones. Revocar el share y cambiar el departamento del destinatario;
   ambos accesos deben desaparecer, mientras propietario y administrador conservan acceso.
6. Como team lead, editar metadata de un documento de su equipo y comprobar que no puede
   reemplazar, eliminar ni compartirlo. Como project manager, eliminar un documento de su proyecto.
7. Asociar un documento a un task y comprobar su aparición en task/project; revisar dashboard con
   cinco recientes y contador.
8. Como administrador, revisar reporte de tipos, uploaders y patrones de acceso; como empleado,
   confirmar que la ruta de reportes está denegada.
