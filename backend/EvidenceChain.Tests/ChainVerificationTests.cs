using EvidenceChain.Domain.Entities;
using EvidenceChain.Domain.Services;
using EvidenceChain.Infrastructure;
using EvidenceChain.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Tests
{
    [Collection("Database collection")]
    public class ChainVerificationTests : IDisposable
    {
        private readonly EvidenceChainDbContext _db;
        private readonly IDbContextTransaction _transaction;

        public ChainVerificationTests(DatabaseFixture fixture)
        {
            var options = new DbContextOptionsBuilder<EvidenceChainDbContext>()
                .UseSqlServer(fixture.ConnectionString)
                .Options;
            _db = new EvidenceChainDbContext(options);
            _transaction = _db.Database.BeginTransaction();
        }

        public void Dispose()
        {
            _transaction.Rollback();
            _db.Dispose();
        }

        [Fact]
        public async Task VerifyAsync_DetectsFirstTamperedEvent()
        {
            var custodian = new Custodian { Id = Guid.NewGuid(), DisplayName = "Test", Role = CustodianRole.Custodio, LoginCode = $"test-{Guid.NewGuid()}" };
            var evidence = new Evidence { Id = Guid.NewGuid(), Code = "EV-TEST-1", Description = "Test", CreatedAtUtc = DateTime.UtcNow, CurrentCustodianId = custodian.Id };

            var e1 = CustodyEvent.Create(evidence.Id, CustodyEventType.Created, custodian.Id, DateTime.UtcNow, EventHasher.Genesis);
            var e2 = CustodyEvent.Create(evidence.Id, CustodyEventType.TransferRequested, custodian.Id, DateTime.UtcNow.AddHours(1), e1.Hash);
            var e3 = CustodyEvent.Create(evidence.Id, CustodyEventType.TransferAccepted, custodian.Id, DateTime.UtcNow.AddHours(2), e2.Hash);

            _db.Custodians.Add(custodian);
            _db.Evidences.Add(evidence);
            _db.CustodyEvents.AddRange(e1, e2, e3);
            await _db.SaveChangesAsync();

            e2.Type = CustodyEventType.TransferRejected;
            await _db.SaveChangesAsync();

            var sut = new ChainVerification(_db);
            var result = await sut.VerifyAsync(evidence.Id);

            Assert.False(result.IsValid);
            Assert.Equal(e2.Id, result.FirstInvalidEventId);
        }

        [Fact]
        public async Task VerifyAsync_ReturnsValid_WhenChainUntouched()
        {
            var custodian = new Custodian { Id = Guid.NewGuid(), DisplayName = "Test2", Role = CustodianRole.Custodio, LoginCode = $"test-{Guid.NewGuid()}" };
            var evidence = new Evidence { Id = Guid.NewGuid(), Code = "EV-TEST-2", Description = "Test", CreatedAtUtc = DateTime.UtcNow, CurrentCustodianId = custodian.Id };
            var e1 = CustodyEvent.Create(evidence.Id, CustodyEventType.Created, custodian.Id, DateTime.UtcNow, EventHasher.Genesis);

            _db.Custodians.Add(custodian);
            _db.Evidences.Add(evidence);
            _db.CustodyEvents.Add(e1);
            await _db.SaveChangesAsync();

            var sut = new ChainVerification(_db);
            var result = await sut.VerifyAsync(evidence.Id);

            Assert.True(result.IsValid);
            Assert.Null(result.FirstInvalidEventId);
        }
    }
}
