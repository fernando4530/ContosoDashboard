# Contrato de almacenamiento documental

La aplicación no expone rutas físicas. `DocumentService` usa una interfaz reemplazable:

```csharp
public interface IFileStorageService
{
    Task<string> WriteTemporaryAsync(Stream content, string extension, CancellationToken cancellationToken);
    Task PromoteAsync(string temporaryKey, string storageKey, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
```

La implementación local resuelve claves bajo el root configurado, verifica que la ruta resultante
permanece dentro de ese root y usa nombres generados por servidor. Debe rechazar claves absolutas,
segmentos `..`, traversal, archivos fuera del root y streams que superen 25 MiB. Una futura
implementación cloud puede usar la misma clave lógica sin cambiar entidades ni workflows.
