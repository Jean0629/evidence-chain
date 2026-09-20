using EvidenceChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvidenceChain.Infrastructure;

public class EvidenceChainDbContext : DbContext
{
    public EvidenceChainDbContext(DbContextOptions<EvidenceChainDbContext> options) : base(options) { }

    public DbSet<Custodian> Custodians => Set<Custodian>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<CustodyEvent> CustodyEvents => Set<CustodyEvent>();
    public DbSet<CustodyTransfer> CustodyTransfers => Set<CustodyTransfer>();
    public DbSet<IdempotencyKeyRecord> IdempotencyKeys => Set<IdempotencyKeyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Concurrencia Optimista
        modelBuilder.Entity<CustodyTransfer>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        // Filtros + Orden
        modelBuilder.Entity<Evidence>()
            .HasIndex(x => new { x.CurrentCustodianId, x.CreatedAtUtc });

        modelBuilder.Entity<CustodyTransfer>()
            .HasOne(x => x.FromCustodian)
            .WithMany()
            .HasForeignKey(x => x.FromCustodianId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustodyTransfer>()
            .HasOne(x => x.ToCustodian)
            .WithMany()
            .HasForeignKey(x => x.ToCustodianId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Evidence>()
            .HasOne(x => x.CurrentCustodian)
            .WithMany()
            .HasForeignKey(x => x.CurrentCustodianId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustodyEvent>()
            .HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustodyEvent>()
            .HasOne(x => x.Evidence)
            .WithMany(ev => ev.Events)
            .HasForeignKey(x => x.EvidenceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Idempotencia
        modelBuilder.Entity<IdempotencyKeyRecord>()
            .HasKey(k => k.Key);
    }
}
