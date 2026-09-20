using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EvidenceChain.Domain.Services
{
    public static class EventHasher
    {
        // Hash inicial
        public const string Genesis = "GENESIS";

        // Formato canónico fijo, separador '|' no puede aparecer en los valores (GUId, DateTime, enum)
        public static string ComputeHash(Guid evidenceId, string eventType, Guid actorId, DateTime occurredAtUtc, string previousHash)
        {
            string dateText = occurredAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            string canonical = $"{evidenceId}|{eventType}|{actorId}|{dateText}|{previousHash}";
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
