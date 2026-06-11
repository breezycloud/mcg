namespace Shared.Enums;

/// <summary>
/// The completion status of a single task within a daily report.
/// Stored as integer in JSONB — values must remain stable.
/// </summary>
public enum TaskItemStatus
{
    Pending    = 0,
    InProgress = 1,
    Done       = 2
}
