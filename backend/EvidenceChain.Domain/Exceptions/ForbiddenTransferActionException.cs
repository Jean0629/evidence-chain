namespace EvidenceChain.Domain.Exceptions
{
    public class ForbiddenTransferActionException() : Exception("Solo el custodio destinatario puede aceptar o rechazar esta transferencia.");
}
