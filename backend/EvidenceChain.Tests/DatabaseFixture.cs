using EvidenceChain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Tests
{
    public class DatabaseFixture
    {
        public string ConnectionString { get; }

        public DatabaseFixture()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            ConnectionString = config.GetConnectionString("Default")!;

            var options = new DbContextOptionsBuilder<EvidenceChainDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            using var db = new EvidenceChainDbContext(options);
            db.Database.EnsureCreated();
        }
    }
}
