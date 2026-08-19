namespace TmsApi.Services;

// M11 Session 1 Exercise 1: demonstrates BCrypt salting mechanics before we
// switch to ASP.NET Core Identity's UserManager for real authentication.
// This service is NOT wired into the real login flow - it exists purely
// to observe that BCrypt.HashPassword produces a different hash every
// time, even for the identical input, because of the random salt it
// generates and embeds in the output.
public class CryptoDemoService
{
    public string HashUserPassword(string plainText)
    {
        // workFactor: 12 means 2^12 key-expansion iterations - deliberately
        // slow, which is what makes brute-forcing every possible password
        // computationally expensive rather than nearly free.
        return BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);
    }

    public bool VerifyUserPassword(string plainText, string hashedDbPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainText, hashedDbPassword);
    }
}
