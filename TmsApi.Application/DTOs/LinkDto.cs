namespace TmsApi.Application.DTOs;

// A single hyperlink in a HATEOAS response.
// Rel = "relation" — a short name describing what this link means
// (e.g. "self", "update", "delete", "enrollments", "enroll").
// Method = the HTTP verb the client should use when following this link.
public record LinkDto(string Href, string Rel, string Method);