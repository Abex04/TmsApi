namespace TmsApi.Dtos;

// What the client sends to POST /api/v2/auth/login.
// M10 Session 2: real password hashing and ASP.NET Core Identity land in
// Module 12 - this is a demo account for testing the cookie transport layer.
public record LoginRequest(string Username, string Password);
