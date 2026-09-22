using EvidenceChain.Domain.Entities;
using EvidenceChain.Domain.Services;
using EvidenceChain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("Default");
var optionsBuilder = new DbContextOptionsBuilder<EvidenceChainDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var db = new EvidenceChainDbContext(optionsBuilder.Options);

Console.WriteLine("Limpiando datos existentes...");
db.CustodyTransfers.RemoveRange(db.CustodyTransfers);
db.CustodyEvents.RemoveRange(db.CustodyEvents);
db.Evidences.RemoveRange(db.Evidences);
db.Custodians.RemoveRange(db.Custodians);
await db.SaveChangesAsync();

// Semilla fija
var rng = new Random(1234);

// ---------- 0. LoginCode ----------
static string ToLoginCode(string displayName, HashSet<string> usedCodes)
{
    var normalized = displayName
        .Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .ToArray();
    var withoutAccents = new string(normalized);
    var parts = withoutAccents.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var baseCode = string.Join(".", parts);

    var code = baseCode;
    var suffix = 1;
    while (!usedCodes.Add(code))
    {
        code = $"{baseCode}{suffix}";
        suffix++;
    }
    return code;
}

// ---------- 1. Custodios ----------
var names = new[] { "Jane Doe", "Richard Roe", "John Smith", "John Doe", "Juan Perez",
    "Luis Gonzalez", "Jose Rodriguez", "Miguel Gomez", "Jorge Fernandez", "Juan Lopez",
    "Sergio Loo", "Marta Delgado", "Tomás Reyes", "Lucía Farfán", "Andrés Cano",
    "Diego Martinez", "Juan Perez" };
var roles = new[] { CustodianRole.Investigador, CustodianRole.Custodio, CustodianRole.Supervisor };

var usedLoginCodes = new HashSet<string>();
var custodians = names.Select((n, i) => new Custodian
{
    Id = Guid.NewGuid(),
    DisplayName = n,
    Role = roles[i % roles.Length],
    LoginCode = ToLoginCode(n, usedLoginCodes)
}).ToList();

db.Custodians.AddRange(custodians);
await db.SaveChangesAsync();
Console.WriteLine($"{custodians.Count} custodios creados.");

var baseDate = DateTime.UtcNow.AddDays(-180);

var investigadores = custodians.Where(c => c.Role == CustodianRole.Investigador).ToList();
var custodios = custodians.Where(c => c.Role == CustodianRole.Custodio).ToList();

// ---------- 2. Función para evidencia "normal" ----------

(Evidence evidence, List<CustodyEvent> events, List<CustodyTransfer> transfers) GenerateEvidence(int index)
{
    var evidence = new Evidence
    {
        Id = Guid.NewGuid(),
        Code = $"EV-{index:D4}",
        Description = $"Evidencia digital recolectada para el caso #{index}",
        CreatedAtUtc = baseDate.AddHours(rng.Next(0, 140 * 24))
    };

    var events = new List<CustodyEvent>();
    var transfers = new List<CustodyTransfer>();

    var current = custodios[rng.Next(custodios.Count)];
    var registeredBy = investigadores[rng.Next(investigadores.Count)];
    var occurredAt = evidence.CreatedAtUtc;
    var prevHash = EventHasher.Genesis;

    var created = CustodyEvent.Create(evidence.Id, CustodyEventType.Created, registeredBy.Id, occurredAt, prevHash);
    events.Add(created);
    prevHash = created.Hash;

    var transferPairs = rng.Next(3, 6); // de 3 a 5 transferencias completas

    for (int p = 0; p < transferPairs; p++)
    {
        occurredAt = occurredAt.AddHours(rng.Next(4, 72));
        var to = custodios.Where(c => c.Id != current.Id).ElementAt(rng.Next(custodios.Count - 1));
        var requestedBy = investigadores[rng.Next(investigadores.Count)];

        var transfer = new CustodyTransfer
        {
            Id = Guid.NewGuid(),
            EvidenceId = evidence.Id,
            FromCustodianId = current.Id,
            ToCustodianId = to.Id,
            Status = TransferStatus.Pending,
            RequestedAtUtc = occurredAt
        };
        transfers.Add(transfer);

        var requested = CustodyEvent.Create(evidence.Id, CustodyEventType.TransferRequested, requestedBy.Id, occurredAt, prevHash, transfer.Id);
        events.Add(requested);
        prevHash = requested.Hash;

        var resolvedAt = occurredAt.AddHours(rng.Next(1, 40));
        var accepted = rng.NextDouble() < 0.85;

        transfer.Status = accepted ? TransferStatus.Accepted : TransferStatus.Rejected;
        transfer.ResolvedAtUtc = resolvedAt;

        var resolutionEvent = CustodyEvent.Create(evidence.Id,
            accepted ? CustodyEventType.TransferAccepted : CustodyEventType.TransferRejected,
            to.Id, resolvedAt, prevHash, transfer.Id);
        events.Add(resolutionEvent);
        prevHash = resolutionEvent.Hash;

        if (accepted) current = to;
        occurredAt = resolvedAt;
    }

    evidence.CurrentCustodianId = current.Id;
    return (evidence, events, transfers);
}

// ---------- 3. Casos especiales ----------
Console.WriteLine("Generando casos especiales (EV-0001, EV-0002, EV-0003)...");

// EV-0001: caso íntegro
var (ev1, events1, transfers1) = GenerateEvidence(1);
db.Evidences.Add(ev1);
db.CustodyEvents.AddRange(events1);
db.CustodyTransfers.AddRange(transfers1);
await db.SaveChangesAsync();
Console.WriteLine($"  EV-0001 (íntegro) — {events1.Count} eventos");

// EV-0002: evento alterado a propósito
var (ev2, events2, transfers2) = GenerateEvidence(2);
db.Evidences.Add(ev2);
db.CustodyEvents.AddRange(events2);
db.CustodyTransfers.AddRange(transfers2);
await db.SaveChangesAsync();

var tampered = events2[Math.Min(1, events2.Count - 1)];
tampered.Type = CustodyEventType.TransferAccepted;
await db.SaveChangesAsync();
Console.WriteLine($"  EV-0002 (evento alterado) — evento {tampered.Id} manipulado sin recalcular hash");

// EV-0003: transferencia vencida
var custodianA = custodios[0];
var custodianB = custodios[1];
var ev3 = new Evidence
{
    Id = Guid.NewGuid(),
    Code = "EV-0003",
    Description = "Evidencia digital recolectada para el caso #3",
    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
    CurrentCustodianId = custodianA.Id
};
var createdEv3 = CustodyEvent.Create(ev3.Id, CustodyEventType.Created, investigadores[0].Id, ev3.CreatedAtUtc, EventHasher.Genesis);
var requestedAt = DateTime.UtcNow.AddDays(-5);
var expiredTransfer = new CustodyTransfer
{
    Id = Guid.NewGuid(),
    EvidenceId = ev3.Id,
    FromCustodianId = custodianA.Id,
    ToCustodianId = custodianB.Id,
    Status = TransferStatus.Pending,
    RequestedAtUtc = requestedAt
};
var requestedEv3 = CustodyEvent.Create(ev3.Id, CustodyEventType.TransferRequested, custodianA.Id, requestedAt, createdEv3.Hash, expiredTransfer.Id);

db.Evidences.Add(ev3);
db.CustodyEvents.AddRange(createdEv3, requestedEv3);
db.CustodyTransfers.Add(expiredTransfer);
await db.SaveChangesAsync();
Console.WriteLine("  EV-0003 (transferencia vencida) — Pending desde hace 5 días");

// ---------- 4. Resto de evidencias (997) en lotes ----------
Console.WriteLine("Generando el resto de evidencias...");
const int batchSize = 100;
for (int i = 4; i <= 1000; i++)
{
    var (evidence, events, transfers) = GenerateEvidence(i);
    db.Evidences.Add(evidence);
    db.CustodyEvents.AddRange(events);
    db.CustodyTransfers.AddRange(transfers);

    if (i % batchSize == 0)
    {
        await db.SaveChangesAsync();
        Console.WriteLine($"  {i}/1000 evidencias guardadas...");
    }
}
await db.SaveChangesAsync();

// ---------- 5. Llegar a los 10000 eventos ----------
var currentTotal = await db.CustodyEvents.CountAsync();
var deficit = 10000 - currentTotal;
var pairsNeeded = deficit / 2;

Console.WriteLine($"Total actual: {currentTotal}. Agregando {pairsNeeded} pares de transferencia adicionales...");

if (pairsNeeded > 0)
{
    var candidateEvidences = await db.Evidences
        .Where(e => e.Code != "EV-0001" && e.Code != "EV-0002" && e.Code != "EV-0003")
        .OrderBy(e => e.Code)
        .Take(pairsNeeded)
        .ToListAsync();

    foreach (var evidence in candidateEvidences)
    {
        var lastEvent = await db.CustodyEvents
            .Where(e => e.EvidenceId == evidence.Id)
            .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
            .FirstAsync();

        var current = await db.Custodians.FirstAsync(c => c.Id == evidence.CurrentCustodianId);
        var to = custodios.Where(c => c.Id != current.Id).ElementAt(rng.Next(custodios.Count - 1));
        var requestedBy = investigadores[rng.Next(investigadores.Count)];

        var occurredAt = lastEvent.OccurredAtUtc.AddHours(1);
        var newTransferId = Guid.NewGuid();
        var requested = CustodyEvent.Create(evidence.Id, CustodyEventType.TransferRequested, requestedBy.Id, occurredAt, lastEvent.Hash, newTransferId);
        db.CustodyEvents.Add(requested);

        var resolvedAt = occurredAt.AddHours(rng.Next(1, 40));
        var accepted = rng.NextDouble() < 0.85;
        var resolutionEvent = CustodyEvent.Create(evidence.Id,
            accepted ? CustodyEventType.TransferAccepted : CustodyEventType.TransferRejected,
            to.Id, resolvedAt, requested.Hash, newTransferId);
        db.CustodyEvents.Add(resolutionEvent);

        db.CustodyTransfers.Add(new CustodyTransfer
        {
            Id = newTransferId,
            EvidenceId = evidence.Id,
            FromCustodianId = current.Id,
            ToCustodianId = to.Id,
            Status = accepted ? TransferStatus.Accepted : TransferStatus.Rejected,
            RequestedAtUtc = occurredAt,
            ResolvedAtUtc = resolvedAt
        });

        if (accepted)
        {
            evidence.CurrentCustodianId = to.Id;
        }
    }

    await db.SaveChangesAsync();
}

var totalAfterPairs = await db.CustodyEvents.CountAsync();
var remaining = 10000 - totalAfterPairs;

if (remaining > 0)
{
    var evidence = await db.Evidences
        .Where(e => e.Code != "EV-0001" && e.Code != "EV-0002" && e.Code != "EV-0003")
        .OrderByDescending(e => e.Code)
        .Take(remaining)
        .ToListAsync();

    foreach (var ev in evidence)
    {
        var lastEvent = await db.CustodyEvents
            .Where(e => e.EvidenceId == ev.Id)
            .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
            .FirstAsync();

        var current = await db.Custodians.FirstAsync(c => c.Id == ev.CurrentCustodianId);
        var to = custodios.Where(c => c.Id != current.Id).ElementAt(rng.Next(custodios.Count - 1));
        var requestedBy = investigadores[rng.Next(investigadores.Count)];
        var fillRequestedAt = DateTime.UtcNow.AddHours(-rng.Next(1, 6));

        var newTransferId = Guid.NewGuid();
        var requested = CustodyEvent.Create(ev.Id, CustodyEventType.TransferRequested, requestedBy.Id, fillRequestedAt, lastEvent.Hash, newTransferId);
        db.CustodyEvents.Add(requested);

        db.CustodyTransfers.Add(new CustodyTransfer
        {
            Id = newTransferId,
            EvidenceId = ev.Id,
            FromCustodianId = current.Id,
            ToCustodianId = to.Id,
            Status = TransferStatus.Pending,
            RequestedAtUtc = fillRequestedAt
        });
    }

    await db.SaveChangesAsync();
}

var totalEvents = await db.CustodyEvents.CountAsync();
var totalEvidences = await db.Evidences.CountAsync();
Console.WriteLine($"Listo: {totalEvidences} evidencias, {totalEvents} eventos.");