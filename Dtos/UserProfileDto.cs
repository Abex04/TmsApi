namespace TmsApi.Dtos;

// What the client receives from a successful login or GET /auth/me.
// Deliberately excludes the token itself - the token lives only in the
// HttpOnly cookie, never in a JSON body JavaScript could read.
public record UserProfileDto(string DisplayName, string Role);
