namespace SwiftDrop.Core.Models;

public sealed record DiagnosticEvent(
    string Id,
    DateTimeOffset TimestampUtc,
    string Level,
    string Code,
    string Message);
