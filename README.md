# Evidence Chain

Aplicación de cadena de custodia forense: revisión de evidencias, verificación de integridad por hash chain, y transferencia de custodia con concurrencia optimista e idempotencia.

## Demo desplegada

- **Frontend**: https://evidencechainweb.vercel.app
- **Backend**: https://evidence-chain-production.up.railway.app
- **Base de datos**: Azure SQL Database

## Stack

- **Backend**: .NET 10, Entity Framework Core, SQL Server
- **Frontend**: React 18+, TypeScript, Vite, TanStack Query, Tailwind CSS
- **Base de datos**: SQL Server

## Estructura del repo

```
/
  backend/
    EvidenceChain.Domain/          Entidades y reglas de negocio puras
    EvidenceChain.Application/     Interfaces de casos de uso
    EvidenceChain.Infrastructure/  EF Core, implementaciones, migraciones
    EvidenceChain.Api/             Controllers, autenticación, Program.cs
    EvidenceChain.Tests/           Tests automatizados de backend
  database/
    EvidenceChain.Seed/            Proyecto de consola con el seed reproducible
  frontend/                        React + TypeScript + Vite
  docs/                            overview.md, decisions.md, code-map.md, ai-usage.md, ai-code-review.md
  openapi.yaml                     Contrato de API generado desde la implementación real
  Dockerfile                       Usado para el despliegue del backend
```

## Levantar el proyecto en local

### Base de datos

Necesitas una instancia de SQL Server. Elige una opción:

**Opción A — LocalDB** (usada en el desarrollo de este proyecto, solo Windows):
```
sqllocaldb start MSSQLLocalDB
```

**Opción B — Docker**:
```
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=TuPassword123!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### Backend

```bash
cp backend/EvidenceChain.Api/appsettings.Example.json backend/EvidenceChain.Api/appsettings.Development.json
# edita appsettings.Development.json con tu connection string (LocalDB o Docker)

dotnet ef database update --project backend/EvidenceChain.Infrastructure --startup-project backend/EvidenceChain.Api

dotnet run --project database/EvidenceChain.Seed

dotnet run --project backend/EvidenceChain.Api
```

La API queda disponible en `https://localhost:44349`, con Swagger en `/swagger`.

### Frontend

```bash
cd frontend
npm install
```

Crea `frontend/.env`:
```
VITE_API_URL=https://localhost:44349/api/v1
```

```bash
npm run dev
```

Disponible en `http://localhost:5173`.

## Ejecutar pruebas

**Backend** (cadena alterada, idempotencia, conflicto de concurrencia, transición inválida):
```bash
dotnet test backend/EvidenceChain.slnx
```

**Frontend** (búsqueda obsoleta, rollback ante 409):
```bash
cd frontend
npm run test
```

## Login de demo

La autenticación es simplificada: usa el `loginCode` de cualquier custodio del seed (ej. `juan.perez`). Puedes consultar los custodios disponibles y su rol directamente en la tabla `Custodians`, o vía `GET /api/v1/custodians` una vez autenticado.

Roles: `Investigador` (crea transferencias), `Custodio` (acepta/rechaza las que le corresponden), `Supervisor` (solo lectura).

## Documentación

Ver la carpeta `docs/`:
- `overview.md` — qué se hizo, qué quedó fuera, cómo se midió la consulta principal
- `decisions.md` — las 4 decisiones técnicas (hash chain, paginación, concurrencia, arquitectura Azure)
- `code-map.md` — tabla de capacidades → archivo → punto de entrada
- `ai-usage.md` — uso de IA durante el desarrollo
- `ai-code-review.md` — revisión del ejercicio AI-REVIEW-01

El contrato completo de la API está en `openapi.yaml`, en la raíz del repo.
