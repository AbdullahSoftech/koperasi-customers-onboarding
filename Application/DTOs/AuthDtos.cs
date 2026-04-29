namespace Application.DTOs;

public record InitiateLoginRequest(string IcNumber);

public record InitiateLoginResponse(
    Guid customerId,
    Guid otpRequestId,
    string PhoneNumber,
    string MaskedEmail,
    Guid LoginSessionId
);

public record CompleteLoginRequest(Guid LoginSessionId, Guid CustomerId, string Pin);

public record LoginResponse(
    Guid CustomerId,
    string FullName,
    string PhoneNumber,
    string Status,
    DateTime LoggedInAt
);