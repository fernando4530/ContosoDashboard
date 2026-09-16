# Modelo de datos

## Document

Entidad con `DocumentId` entero. Campos: `Title` requerido (255), `Description` opcional,
`Category` requerido como texto (los seis valores de la especificación), `Tags` opcional,
`OriginalFileName` solo para presentación, `StorageKey` privado/relativo, `FileType` MIME hasta
255, `FileSize` entero largo, `UploadedDate` UTC, `UploaderId` requerido, `ProjectId` opcional,
`TaskId` opcional y estado de disponibilidad (`Pending`, `Ready`, `Failed`).

Un documento debe estar `Ready` para listarse, descargarse, previsualizarse o compartirse.
`StorageKey` nunca se deriva directamente del nombre enviado por el usuario.

## DocumentShare

Entidad con ID entero, `DocumentId`, `RecipientUserId` nullable, `RecipientDepartment` nullable,
`SharedByUserId`, `CreatedDate`, `RevokedDate` nullable y restricciones para exigir exactamente
un destinatario. El departamento representa un share a equipo según la convención actual de
`UserService`; se materializa al evaluar permisos, no como una copia de usuarios.

## DocumentActivity

Entidad con ID entero, `DocumentId` nullable para conservar eventos posteriores a eliminación,
`ActorUserId`, `Action` textual (`Upload`, `Download`, `Delete`, `Share`), `OccurredDate` UTC,
`Details` opcional y metadatos mínimos no sensibles para informes.

## Relaciones y reglas

- `User 1:N Document` y `Project 1:N Document` con borrado restringido o nulificación explícita
  para conservar claridad de auditoría.
- `TaskItem 1:N Document` opcional; al asociar desde una tarea se valida que la tarea pertenezca
  al proyecto indicado.
- `Document 1:N DocumentShare` y `Document 1:N DocumentActivity`.
- Índices: uploader/fecha, project/fecha, category, status, `FileType`, shares activas por
  documento/destinatario y actividad por fecha/acción.
- Integridad: título/categoría no vacíos; tamaño entre 1 y 25 MiB; `StorageKey` único; un
  documento Ready debe tener archivo; shares revocadas no conceden acceso.
- Las consultas de lista, búsqueda, preview y descarga deben aplicar la misma predicado de
  autorización antes de materializar resultados.
