namespace BLL.Encryption;


public class PassHash
{
    // Fallback value only for development - MUST be set in production via PASSWORD_PEPPER env var
    private static string pepper = Environment.GetEnvironmentVariable("PASSWORD_PEPPER") 
        ?? throw new InvalidOperationException("PASSWORD_PEPPER environment variable must be set for security");
    
    public static string GetRandomSalt()
    {
        return BCrypt.Net.BCrypt.GenerateSalt(12);
    }
    public static string HashPassword(string password)
    {
        string PassWPepper = password + pepper;
        return BCrypt.Net.BCrypt.HashPassword(PassWPepper, GetRandomSalt());
    }
    public static bool ValidatePassword(string password, string correctHash)
    {
        {
            var verify = BCrypt.Net.BCrypt.Verify(password + pepper, correctHash);
            return verify;

        }
    }
}