namespace Application.DTOs;

public record RegisterCustomerRequest(
    string FullName,
    string IcNumber,
    string MobileNumber,
    string EmailAddress
);

public record InitiateRegistrationResponse(
    Guid CustomerId,
    Guid OtpRequestId,
    DateTime ExpiresAt
);

public record MigrateCustomerRequest(
    Guid OtpRequestId,
    string PhoneNumber,
    string? OldSystemRef
);

public record SetupPinRequest(string Pin);

public record BiometricRequest(string BiometricToken);

public record ConsentRequest(string PolicyVersion, bool IsAccepted);

public record CustomerResponse(
    Guid Id,
    string PhoneNumber,
    string Email,
    string FullName,
    string Status,
    string CustomerType,
    DateTime CreatedAt
);
