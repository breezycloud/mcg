using System;

namespace Shared.Models.Auth;

// Only the SHA-256 hash of the actual token is ever stored — matches the reset-token pattern
// already used elsewhere (Auth.ForgotPassword), so a DB read/leak never exposes a usable token.
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    // Rotation chain: when a token is used to refresh, it's revoked and replaced by a new one —
    // if a caller ever presents an already-revoked token, that's a strong signal of a stolen/
    // replayed token, not just an expired session.
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
