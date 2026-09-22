# ai-usage.md

## Herramientas de IA utilizadas y en qué partes

- **Claude (conversación)**: usado durante el desarrollo backend — diseño del modelo de dominio, seed de datos, revisión de arquitectura por capas, y debugging de errores de compilación y de runtime (EF Core, migraciones, SQL Server).
- **Claude Design**: usado para el prototipo inicial del frontend.
- **Claude Code**: usado para acelerar la construcción del frontend en React, 
  a partir de un brief (contrato API, lógica de negocio, vistas necesarias) redactado posterior al desarrollo del backend.

## Una sugerencia que acepté y por qué

Para el seed reproducible, inicialmente propuse un script .sql. La sugerencia fue construirlo como un proyecto de consola en C# que referencia directamente `Domain` e `Infrastructure`.

La acepté porque el seed necesita calcular el mismo hash encadenado que calcula la API. Un proyecto de C# puede reutilizar exactamente el mismo `EventHasher.ComputeHash` y `CustodyEvent.Create` que usa el backend.

## Una sugerencia que rechacé o corregí, por qué era inadecuada y qué hice en su lugar

Al implementar la creación de una transferencia de custodia, la primera versión asignaba `FromCustodianId` (de quién sale la evidencia) usando el `Id` del Investigador autenticado que solicitaba la transferencia.

Esto era incorrecto: según la lógica definida el Investigador solo es el autor de la transferencia más no un custodio.

Lo corregí separando ambos conceptos: `FromCustodianId/ToCustodianId` en la 
transferencia se obtienen siempre de Evidence.CurrentCustodianId (el custodio 
real), mientras que el ActorId del evento de auditoría correspondiente registra 
quién ejecutó la acción (el Investigador).

## Una parte que revisé especialmente antes de aceptar código generado

Al generar el código para la clase que obtiene las evidencias paginadas rompía la regla de dependencias de Clean Architecture referenciando la capa `Infrastructure` desde `Application`. Lo solucioné generando las Interfaces desde `Application` e implementandolas en `Infrastructure` siguiendo la regla de dependencias.
