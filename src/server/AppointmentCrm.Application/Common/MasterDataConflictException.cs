namespace AppointmentCrm.Application.Common;

public sealed class MasterDataConflictException(string message) : InvalidOperationException(message);
