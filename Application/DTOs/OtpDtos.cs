using Domain.Enums;

namespace Application.DTOs;

public record SendOtpRequest(string PhoneNumber, OtpPurpose Purpose);

public record VerifyOtpRequest(Guid OtpRequestId, string PhoneNumber, string OtpCode, Guid? LoginSessionId = null);

public record SendEmailOtpRequest(Guid CustomerId, OtpPurpose Purpose, Guid? LoginSessionId = null);

public record VerifyEmailOtpRequest(Guid CustomerId, Guid OtpRequestId, string OtpCode, Guid? LoginSessionId = null);

public record OtpResponse(Guid OtpRequestId, DateTime ExpiresAt);