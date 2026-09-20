using EvidenceChain.Domain.Entities;
using EvidenceChain.Domain.Services;
using EvidenceChain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

// ---------- 1. Custodios ----------
var names = new[] { "Jane Doe", "Richard Roe", "John Smith", "John Doe", "Juan Perez",
    "Luis Gonzalez", "Jose Rodriguez", "Miguel Gomez", "Jorge Fernandez", "Juan Lopez",
    "Sergio Loo", "Marta Delgado", "Tomás Reyes", "Lucía Farfán", "Andrés Cano",
    "Diego Martinez" };
var roles = new[] { CustodianRole.Investigador, CustodianRole.Custodio, CustodianRole.Supervisor };

var custodians = names.Select((n, i) => new Custodian
{
    Id = Guid.NewGuid(),
    DisplayName = n,
    Role = roles[i % roles.Length]
}).ToList();

db.Custodians.AddRange(custodians);
await db.SaveChangesAsync();
Console.WriteLine($"{custodians.Count} custodios creados.");

var baseDate = DateTime.UtcNow.AddDays(-180);

// ---------- 2. Función para evidencia "normal" ----------
(Evidence evidence, List<CustodyEvent> events, List<CustodyTransfer> transfers) GenerateEvidence(int index)
{
    var evidence = new Evidence
    {
        Id = Guid.NewGuid(),
        Code = $"EV-{index:D4}",
        Description = $"Evidencia digital recolectada para el caso #{index}",
        CreatedAtUtc = baseDate.AddHours(rng.Next(0, 180 * 24))
    };

    var events = new List<CustodyEvent>();
    var transfers = new List<CustodyTransfer>();

    var current = custodians[rng.Next(custodians.Count)];
    var occurredAt = evidence.CreatedAtUtc;
    var prevHash = EventHasher.Genesis;

    var created = CustodyEvent.Create(evidence.Id, CustodyEventType.Created, current.Id, occurredAt, prevHash);
    events.Add(created);
    prevHash = created.Hash;

    var targetEvents = rng.Next(8, 13); // ~10 en promedio
    var count = 1;

    while (count < targetEvents)
    {
        occurredAt = occurredAt.AddHours(rng.Next(4, 72));
        var to = custodians.Where(c => c.Id != current.Id).ElementAt(rng.Next(custodians.Count - 1));

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

        var requested = CustodyEvent.Create(evidence.Id, CustodyEventType.TransferRequested, current.Id, occurredAt, prevHash);
        events.Add(requested);
        prevHash = requested.Hash;
        count++;

        if (count >= targetEvents) break; // queda pendiente

        var resolvedAt = occurredAt.AddHours(rng.Next(1, 40));
        var accepted = rng.NextDouble() < 0.85;

        transfer.Status = accepted ? TransferStatus.Accepted : TransferStatus.Rejected;
        transfer.ResolvedAtUtc = resolvedAt;

        var resolutionEvent = CustodyEvent.Create(evidence.Id,
            accepted ? CustodyEventType.TransferAccepted : CustodyEventType.TransferRejected,
            to.Id, resolvedAt, prevHash);
        events.Add(resolutionEvent);
        prevHash = resolutionEvent.Hash;
        count++;

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
var custodianA = custodians[0];
var custodianB = custodians[1];
var ev3 = new Evidence
{
    Id = Guid.NewGuid(),
    Code = "EV-0003",
    Description = "Evidencia digital recolectada para el caso #3",
    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
    CurrentCustodianId = custodianA.Id
};
var createdEv3 = CustodyEvent.Create(ev3.Id, CustodyEventType.Created, custodianA.Id, ev3.CreatedAtUtc, EventHasher.Genesis);
var requestedAt = DateTime.UtcNow.AddDays(-5);
var requestedEv3 = CustodyEvent.Create(ev3.Id, CustodyEventType.TransferRequested, custodianA.Id, requestedAt, createdEv3.Hash);
var expiredTransfer = new CustodyTransfer
{
    Id = Guid.NewGuid(),
    EvidenceId = ev3.Id,
    FromCustodianId = custodianA.Id,
    ToCustodianId = custodianB.Id,
    Status = TransferStatus.Pending,
    RequestedAtUtc = requestedAt
};

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

var totalEvents = await db.CustodyEvents.CountAsync();
var totalEvidences = await db.Evidences.CountAsync();
Console.WriteLine($"Listo: {totalEvidences} evidencias, {totalEvents} eventos.");