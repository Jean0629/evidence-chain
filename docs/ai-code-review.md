# AI-REVIEW-01 — Revisión de código asistido

## `AcceptPendingTransfersAsync`

```csharp
public async void AcceptPendingTransfersAsync()
{
    var transfers = await _db.CustodyTransfers
        .Where(transfer => transfer.Status == TransferStatus.Pending)
        .ToListAsync();

    await Task.WhenAll(transfers.Select(async transfer =>
    {
        transfer.AcceptedAtUtc = DateTime.Now;
        transfer.Status = TransferStatus.Accepted;
        await _db.SaveChangesAsync();
    }));
}
```

| # | Defecto | Severidad | Impacto | Corrección propuesta |
|---|---|---|---|---|
| 1 | `async void` en vez de `async Task` | **Alta** | Ninguna excepción lanzada dentro del método puede ser capturada por el llamador — si algo falla, la excepción queda "no observada" y puede terminar el proceso de forma abrupta en vez de propagarse como una falla controlada. Tampoco se puede `await` la finalización real del método desde quien lo invoca. | Cambiar la firma a `public async Task AcceptPendingTransfersAsync()`. `async void` solo es apropiado en manejadores de eventos de UI, nunca en lógica de negocio o backend. |
| 2 | `_db.SaveChangesAsync()` invocado desde varias tareas superpuestas en el tiempo, sobre la **misma instancia** de `DbContext` | **Crítica** | `DbContext` no admite operaciones asíncronas superpuestas sobre sí mismo (no es seguro para ese patrón, con o sin múltiples hilos de CPU de por medio). Como cada lambda dentro de `Task.WhenAll` corre hasta su primer `await` antes de ceder el control, varias llamadas a `SaveChangesAsync()` quedan pendientes al mismo tiempo contra el mismo contexto, produciendo `InvalidOperationException` ("A second operation was started on this context instance before a previous operation completed") de forma intermitente, o resultados inconsistentes si el error no se maneja. | No superponer llamadas a `SaveChangesAsync()` sobre el mismo `DbContext`. Aplicar los cambios en memoria dentro de un `foreach` secuencial (sin `Task.WhenAll` sobre el propio contexto) y llamar a `SaveChangesAsync()` una sola vez, fuera del loop, para persistir todos los cambios en un solo batch. Si se requiere paralelismo real, cada tarea necesitaría su propia instancia de `DbContext`. |
| 3 | `DateTime.Now` en vez de `DateTime.UtcNow` | **Media** | Un sistema que registra eventos con marcas de tiempo debería usar una zona horaria única y consistente (UTC) para que las comparaciones y el orden entre eventos sean confiables, independientemente de en qué servidor o zona horaria se ejecute el proceso. Usar hora local introduce ambigüedad y puede producir comparaciones incorrectas si el servidor cambia de horario (horario de verano) o si la aplicación se despliega en otra región. | Usar `DateTime.UtcNow` de forma consistente en cualquier marca de tiempo que se persista o se compare. |
| 4 | La transición de estado se realiza por asignación directa de campo (`transfer.Status = ...`), sin validar que la transición sea legítima desde el estado actual, ni verificar que el registro no haya cambiado desde que se leyó | **Alta** | Cualquier código con acceso al objeto puede moverlo a cualquier estado, sin que exista una regla explícita que confirme que esa transición es válida — por ejemplo, nada impediría que este método se ejecutara dos veces sobre el mismo registro, o que se llamara mientras otro proceso también lo estaba modificando, sin que ninguna de las dos ejecuciones se entere de la otra. | Encapsular la transición de estado detrás de un método que valide explícitamente el estado de origen antes de aplicar el cambio (una máquina de estados), y usar una columna de control de concurrencia (versión/timestamp) al persistir, de forma que si el registro cambió entre la lectura y la escritura, la operación falle explícitamente en vez de sobrescribir en silencio. |
| 5 | No hay verificación de que quien ejecuta la operación tenga autorización sobre cada transferencia específica | **Alta** | El método procesa todas las transferencias pendientes del sistema sin distinguir a quién pertenece cada una ni quién solicitó la operación. Si este método se expone como una acción accesible por distintos usuarios, permitiría que cualquiera resolviera transferencias que no le corresponden. | Verificar, para cada registro, que el actor autenticado tiene permiso sobre ese recurso específico antes de aplicar la acción — no asumir que "poder llamar al método" equivale a "tener autorización sobre todos los datos que toca". |
| 6 | No se genera ningún registro de auditoría de la operación realizada | **Media** | Si el sistema necesita poder reconstruir después "qué cambió, cuándo y por acción de quién" (algo esperable en un dominio donde se rastrea custodia o cumplimiento), este método deja el cambio de estado sin ningún rastro más allá del valor final en la fila — no queda evidencia de la transición en sí. | Registrar un evento de auditoría (quién, qué cambió, cuándo) como parte de la misma operación que actualiza el estado, no como un paso opcional separado. |

## `Search`

```csharp
public IEnumerable<CustodyTransfer> Search(string name)
{
    return _db.CustodyTransfers
        .FromSqlRaw($"SELECT * FROM CustodyTransfers WHERE CustodianName = '{name}'")
        .ToList();
}
```

| # | Defecto | Severidad | Impacto | Corrección propuesta |
|---|---|---|---|---|
| 7 | Inyección SQL vía interpolación de string directa dentro de `FromSqlRaw` | **Crítica** | El parámetro `name` se concatena sin sanitizar dentro del texto SQL. Un valor como `' OR '1'='1` o `'; DROP TABLE CustodyTransfers; --` se ejecutaría literalmente contra la base de datos. Es una vulnerabilidad explotable desde cualquier input que llegue a este método sin validar. | Usar `FromSqlInterpolated($"SELECT * FROM CustodyTransfers WHERE CustodianName = {name}")` — con la sintaxis de interpolación `$""`, EF Core parametriza automáticamente los valores, a diferencia de `FromSqlRaw`, donde la responsabilidad de parametrizar recae enteramente en quien escribe el código. Preferible aún: reemplazar el SQL crudo por una consulta LINQ estándar, que se parametriza de forma segura por diseño. |
| 8 | La consulta compara por `CustodianName` | **Media** | Si hay nombres de custodios duplicados esta consulta devolvería resultados de más de un custodio. Además la información del custodio debería vivir en una tabla aparte no en la tabla de transferencias. |
| 9 | Método síncrono (`ToList()`) en un método que retorna resultados de una consulta a base de datos | **Baja** | Bloquea el hilo que lo ejecuta mientras espera la respuesta de la base de datos. En un contexto de alta concurrencia (por ejemplo, un servidor web procesando muchas solicitudes), esto reduce el throughput disponible, ya que el hilo queda ocupado esperando I/O en vez de liberarse para atender otras solicitudes. | Exponer el método como `async Task<List<CustodyTransfer>>` usando `ToListAsync()`, permitiendo que el hilo se libere mientras se espera la respuesta de la base de datos. |
