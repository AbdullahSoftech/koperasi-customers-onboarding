using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Wrappers;

namespace API.Controllers;

[ApiController]
[Route("api/content")]
public class ContentController : ControllerBase
{
    /// <summary>
    /// Get the current privacy policy text.
    /// </summary>
    [HttpGet("privacy-policy")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult GetPrivacyPolicy()
    {
        // In a real app this would come from a CMS or database.
        // Mocked here as per task requirements (no 3rd party integrations).
        var policy = new
        {
            Version = AppConstants.DefaultPolicyVersion,
            Title = "Privacy Policy",
            EffectiveDate = "2025-01-01",
            Content = """
                1. INTRODUCTION
                We are committed to protecting your personal information and your right to privacy.

                2. INFORMATION WE COLLECT
                We collect personal information that you voluntarily provide when registering,
                including your name, phone number, email address, date of birth, and national ID.

                3. HOW WE USE YOUR INFORMATION
                We use the information we collect to provide, operate, and improve our services,
                process transactions, and communicate with you.

                4. DATA SECURITY
                We implement appropriate technical and organizational security measures to protect
                your personal information against unauthorized access, alteration, disclosure, or destruction.

                5. YOUR RIGHTS
                You have the right to access, correct, or delete your personal information at any time.
                Contact our support team to exercise these rights.

                6. CONTACT US
                If you have questions about this Privacy Policy, please contact us through the app.
                """
        };

        return Ok(ApiResponse<object>.Ok(policy, "Privacy policy retrieved successfully."));
    }
}