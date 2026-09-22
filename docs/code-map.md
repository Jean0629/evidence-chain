# code-map.md

| Capacidad | Archivo / módulo | Punto de entrada |
|---|---|---|
| Encadenado de hash | `backend/EvidenceChain.Domain/Services/EventHasher.cs` (`ComputeHash`), `backend/EvidenceChain.Domain/Entities/CustodyEvent.cs` (`CustodyEvent.Create`) | Se invoca desde `CustodyTransferService` (backend/EvidenceChain.Infrastructure/Transfers/) cada vez que se crea o resuelve una transferencia, y desde `database/EvidenceChain.Seed/Program.cs` al generar datos |
| Verificación de hash | `backend/EvidenceChain.Infrastructure/Queries/ChainVerification.cs` (`VerifyAsync`) | `GET /api/v1/evidence/{id}/chain/verify` y desde `backend/EvidenceChain/Infrastructure/Queries/EvidenceQueries.cs` (`GetPageAsync`) para mostrar el estado de integridad en la lista de evidencias |
| Máquina de estados | `backend/EvidenceChain.Domain/Entities/CustodyTransfer.cs` (`Accept()`, `Reject()`), `backend/EvidenceChain.Domain/Exceptions/InvalidTransitionException.cs` | `POST /api/v1/custody-transfers/{id}/accept`, `POST /api/v1/custody-transfers/{id}/reject` |
| Concurrencia optimista y 409 | `backend/EvidenceChain.Infrastructure/Transfers/CustodyTransferService.cs` (`ResolveAsync`), `backend/EvidenceChain.Domain/Exceptions/ConcurrencyConflictException.cs`, manejo de excepciones en `backend/EvidenceChain.Api/Program.cs` | Header `If-Match` en `POST /accept` y `POST /reject`, columna `RowVersion` en `CustodyTransfer` |
| Idempotencia | `backend/EvidenceChain.Infrastructure/Entities/IdempotencyKeyRecord.cs`, `CustodyTransferService.CreateAsync` | Header `Idempotency-Key` en `POST /api/v1/custody-transfers` |
| Regla de anomalía | `backend/EvidenceChain.Infrastructure/Queries/EvidenceQueries.cs` (`GetByIdAsync`), configuración `TransferPolicy:ExpirationHours` en `appsettings.json` | `GET /api/v1/evidence/{id}` (campos `hasAnomaly`, `anomalySeverity`, `anomalyReason`) |
| Consulta paginada (keyset) | `backend/EvidenceChain.Infrastructure/Queries/EvidenceQueries.cs` (`GetPageAsync`) | `GET /api/v1/evidence?search=&custodianId=&cursor=&sort=` |
| Autenticación y roles | `backend/EvidenceChain.Infrastructure/Auth/TokenService.cs`, atributos `[Authorize(Roles = "...")]` en los controllers | `POST /api/v1/auth/login`; header `Authorization: Bearer` en el resto de endpoints |
| Filtros de URL y cancelación de solicitudes | `frontend/src/features/evidence-list/` (hook de bandeja sobre `useSearchParams` + React Query) | Página `/evidence` |
| Actualización optimista y manejo de 409 | `frontend/src/features/custody-transfer/` (hook de mutación con `onMutate`/`onError`) | Modal de transferencia, y acciones de aceptar/rechazar en el detalle de evidencia |
| Pruebas automatizadas | `backend/EvidenceChain.Tests/` (`ChainVerificationTests.cs`, `IdempotencyTests.cs`, `ConcurrencyTests.cs`) | `dotnet test backend/EvidenceChain.sln` |
