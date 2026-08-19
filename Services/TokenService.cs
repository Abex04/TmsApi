using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TmsApi.Identity;

namespace TmsApi.Services;

public class TokenService(IConfiguration config)
{
    public string GenerateJwt(TmsUser user, IList<string> roles)
    {
        // Literal short claim names ("sub", "email", "role") - matches
        // what a JWT decoder (like jwt.ms) shows by convention, and
        // matches the PDF's verification checkpoint exactly. This only
        // works because Program.cs clears
        // JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap at startup;
        // without that, "sub" would silently become a long schema URI.
        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("email", user.Email ?? string.Empty),
            new("FirstName", user.FirstName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(config["Jwt:ExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
