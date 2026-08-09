namespace SwiftDrop.Core.Diagnostics;

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record NetworkDiagnostic(
    string Code,
    string Title,
    string Message,
    DiagnosticSeverity Severity);
