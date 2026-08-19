namespace TmsApi.Entities;

// M11 Session 2: backs refresh-token rotation. IsUsed flips to true the
// moment this token is redeemed for a new pair - a second attempt to use
// the SAME token again is what triggers theft detection (see
// AuthController.Refresh).
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
}
