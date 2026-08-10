namespace SwiftDrop.Core.Protocol;

public static class TransferResponsePolicy
{
    public static long ValidateResumeOffset(bool accepted, long offset, long expectedLength, string? message)
    {
        if (!accepted) throw new IOException(message ?? "Receiver rejected the transfer.");
        if (expectedLength < 0) throw new ArgumentOutOfRangeException(nameof(expectedLength));
        if (offset < 0 || offset > expectedLength)
            throw new InvalidDataException("Receiver returned an invalid resume offset.");
        return offset;
    }

    public static void ValidateCompletion(bool accepted, long completedLength, long expectedLength, string? message)
    {
        if (!accepted) throw new IOException(message ?? "Receiver reported transfer failure.");
        if (expectedLength < 0) throw new ArgumentOutOfRangeException(nameof(expectedLength));
        if (completedLength != expectedLength)
            throw new InvalidDataException("Receiver completion length did not match the expected transfer length.");
    }

    public static void ValidateTextAcknowledgement(bool accepted, long offset, string? message)
    {
        if (!accepted) throw new IOException(message ?? "Receiver rejected the text snippet.");
        if (offset != 0)
            throw new InvalidDataException("Receiver returned an invalid text acknowledgement offset.");
    }
}
