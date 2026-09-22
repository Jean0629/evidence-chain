using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Tests
{
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
}
