namespace Authentication.Application.Tokens;

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);