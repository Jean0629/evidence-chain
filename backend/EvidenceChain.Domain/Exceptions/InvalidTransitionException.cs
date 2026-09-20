using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Domain.Exceptions
{
    public class InvalidTransitionException(string currentStatus, string attemptedAction) : Exception($"No se puede {attemptedAction} una transferencia en estado '{currentStatus}'.")
    {
        public string CurrentStatus { get; } = currentStatus;
    }
}
