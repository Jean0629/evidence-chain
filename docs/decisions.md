# decisions.md

## 1. Persistencia y encadenado de hash

**Opción elegida**: SHA-256 sobre un formato canónico de concatenación fija: `evidenceId|eventType|actorId|dateText|previousHash`, separado por `|`. Cada campo es de formato controlado (GUID, nombre de enum, fecha ISO con `Z` y hash previo), por lo que ninguno puede contener el separador. El primer evento de cada evidencia usa la constante `GENESIS` como `previousHash` (en producción se puede usar el hash del archivo real para no solo garantizar los eventos de custodia sino la evidencia en sí). Los eventos viven en una tabla append-only (`CustodyEvents`), separada de la entidad mutable de negocio (`CustodyTransfer`), porque un mismo registro no puede ser simultáneamente inmutable (para el hash chain) y mutable (para la máquina de estados con control de concurrencia).

**Alternativa descartada**: serializar cada evento a JSON canónico (claves ordenadas). Es más robusto frente a texto libre, pero innecesario aquí porque ningún campo hasheado es texto libre — se prefirió la opción más simple que cumple el mismo objetivo sin la sobrecarga de una librería de serialización canónica.

**Costo asumido**: `/chain/verify` recalcula el hash de cada evento de la evidencia en cada llamada — O(n) sobre el número de eventos de esa evidencia específica, no sobre el total del sistema. A la escala actual es despreciable.

**Señal para cambiar**: si el número de eventos por evidencia creciera a miles, la verificación completa en cada llamada se volvería costosa. La señal sería un tiempo de respuesta creciente en `/chain/verify` — ahí convendría introducir un snapshot periódico del "último hash validado" para no recalcular la cadena completa desde el génesis en cada verificación.

## 2. Paginación

**Opción elegida**: keyset pagination (seek method), con un cursor codificado como `fecha_id` (el `Id` como desempate cuando dos filas comparten la misma fecha), y `ORDER BY fecha, id` (`ASC` o `DESC` según el parámetro de orden). La comparación del cursor se invierte según la dirección (`<` para descendente, `>` para ascendente).

**Alternativa descartada**: offset/limit. Permite saltar a una página arbitraria, pero con miles de filas cada `OFFSET` grande obliga a SQL Server a recorrer y descartar las filas salteadas, degradando con el tamaño de la tabla. Adems ess inestable si se inserta una fila nueva mientras alguien pagina (una fila puede repetirse o saltarse entre páginas).

**Costo asumido**: no es posible saltar directamente a una página arbitraria (solo "siguiente", de forma secuencial). Cambiar la dirección de orden (asc↔desc) mientras se navega obliga a resetear a la primera página, porque un cursor tomado en una dirección no es válido para continuar en la contraria.

**Señal para cambiar**: si el producto necesitara una UI de "ir a la página N" con frecuencia real de uso, keyset dejaría de ser suficiente por sí solo — se evaluaría un híbrido (offset solo para saltos grandes, keyset para navegación secuencial) o aceptar el costo de offset con los índices adecuados.

## 3. Concurrencia y actualización optimista

**Opción elegida**: columna `rowversion` (`RowVersion`) en `CustodyTransfer`, expuesta al cliente como `ETag`. El cliente debe enviar `If-Match` con el valor recibido en la última lectura; el servidor fija ese valor como `OriginalValue` antes de `SaveChangesAsync()`, y EF Core detecta el desajuste como `DbUpdateConcurrencyException`, mapeado a `409` con `currentStatus`. La validación de la máquina de estados ocurre en memoria **antes** de intentar guardar — así una transición ya inválida por el estado actual (ej. rechazar algo ya aceptado) devuelve un error específico de transición inválida, y el conflicto de versión solo se reporta cuando la transición en sí era legítima pero otra escritura llegó primero.

**Alternativa descartada**: Bloquear la fila al leerla. Se descartó por la contención y complejidad operativa adicional que introduce, innecesaria para el volumen de escrituras concurrentes esperado en este dominio (transferencias de custodia no son una operación de muy alta frecuencia por evidencia).

**Costo asumido**: ante un `409`, el cliente debe volver a consultar el estado real antes de reintentar — no hay reconciliación automática del lado del servidor. Esto traslada complejidad al frontend (reversión del estado optimista, nueva consulta).

**Señal para cambiar**: si en producción se observara una tasa alta de conflictos reales (no producto de bugs, sino de usuarios genuinamente compitiendo por el mismo recurso con frecuencia), valdría la pena evaluar locking pesimista o una cola de resolución secuencial para esas transferencias específicas.

## 4. Arquitectura Azure de producción

**Cómputo**: Azure App Service (Linux), tier Basic/Standard, para la API .NET. despliegue directo desde el pipeline sin gestionar contenedores ni orquestación, con deployment slots para swap sin downtime entre staging y producción.

**Frontend**: En una arquitectura de producción completa sobre Azure, se propondría **Azure Static Web Apps**, tier gratuito generoso para este volumen de tráfico, y CI/CD directo desde GitHub.

**Azure SQL Database**: tier General Purpose para producción (Serverless con auto-pause para staging/desarrollo, donde el tráfico es intermitente). Mismo motor que el usado en desarrollo local, por lo que las migraciones de EF Core y el seed corren sin cambios.

**Blob Storage**: reservado para la subida de archivos. Tier Hot para acceso reciente, con política de ciclo de vida moviendo a Cool/Archive tras un período sin acceso.

**Key Vault**: para el secreto de firma JWT y la connection string de producción, referenciados desde App Service vía Managed Identity.

**Application Insights**: integrado directamente con App Service para logging estructurado, tracing de requests, y como fuente de las alertas operativas.

**Separación de ambientes**: Desarrollo (LocalDB local, sin costo), Staging (Azure SQL Serverless + slot "staging" de App Service, secretos propios en Key Vault), Producción (Azure SQL General Purpose + slot "production", swap desde staging tras validación).

**Primer indicador/alerta operativa**: alerta sobre la tasa de errores `5xx` y de `409` de concurrencia en Application Insights (Failure Rate), una alerta específica de latencia sobre `/chain/verify`, ya que un crecimiento en el tiempo de respuesta de ese endpoint es la primera señal de que la estrategia de verificación necesita revisarse, y una alerta de latencia específica sobre `/evidence`, ya que calcula fila por fila (sobre la página visible) el estado de integirdad de la cadena de custodia.

**Estimación mensual para un equipo pequeño**: App Service Basic B1 (~US$13–55 según región), Azure Static Web Apps tier free, Azure SQL Serverless con auto-pause para baja carga (~US$5–15), Application Insights (gratuito hasta 5 GB/mes, esperado US$0 a esta escala), Key Vault (prácticamente gratuito, ~US$0.03 por 10,000 operaciones), Blob Storage solo si se activa el extra (~US$0.02/GB en Hot). Total aproximado: **US$20–70/mes** en un tier de desarrollo activo, escalando a Standard/Premium si se requiriera un SLA de producción real. Se comparte link a la calculadora de azure: [calculadora](https://azure.com/e/6356338a0106480fa4e386e96d22630e)
