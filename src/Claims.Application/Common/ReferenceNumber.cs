namespace Claims.Application.Common;

/// <summary>
/// Generates human-readable, reasonably unique business reference numbers, e.g.
/// "CLM-20260609-AB12CD". Uniqueness is additionally guarded by a repository
/// existence check at creation time.
/// </summary>
public static class ReferenceNumber
{
    public static string ForClaim(DateOnly today) => Build("CLM", today);
    public static string ForPolicy(DateOnly today) => Build("POL", today);

    private static string Build(string prefix, DateOnly today)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{today:yyyyMMdd}-{suffix}";
    }
}
