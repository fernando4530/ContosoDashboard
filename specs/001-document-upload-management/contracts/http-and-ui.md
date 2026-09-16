# Contrato HTTP y UI

## Operaciones autenticadas

- `GET /documents/{documentId}/download`: busca el documento por ID, autoriza y devuelve el
  stream como `attachment` con el MIME validado; denegación y documento inexistente no revelan
  la clave física.
- `GET /documents/{documentId}/preview`: misma autorización; solo PDF/JPEG/PNG y disposición
  `inline`; formatos no soportados devuelven una respuesta de operación no disponible.
- Las páginas Blazor `/documents`, `/documents/shared` y `/admin/document-reports` delegan todas
  las consultas y mutaciones al servicio, no a `ApplicationDbContext` directamente.

## Comportamiento de la interfaz

- La carga acepta múltiples archivos, muestra estado/progreso por archivo y devuelve éxito o
  motivo específico por cada resultado.
- El listado combina búsqueda por título, descripción, tags, uploader y proyecto con filtros por
  categoría/proyecto/fechas y orden por título, fecha, categoría o tamaño; siempre recibe datos
  ya autorizados.
- Acciones visibles dependen de permisos calculados por servicio: propietario puede metadata,
  reemplazo, borrado y compartir; project manager puede gestionar documentos de su proyecto;
  team lead puede ver/descargar/editar metadata de documentos de su equipo, pero no reemplazar,
  borrar ni compartir; administrador puede revisar y administrar según la política global.
- El dashboard agrega cinco recientes y contador; project/task details agregan documentos
  autorizados; compartir y agregar al proyecto producen notificaciones en la vista existente.
