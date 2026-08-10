using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class ApplicationProtocolConversationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private const string Nonce = "ABCDEFGHIJKLMNOPQRSTUVWX";

    [Fact]
    public async Task FileConversation_RoundTripsAuthorizationResumeAndCompletionFrames()
    {
        var entry = Entry("file.bin", 10, 'A');
        var request = ProtocolRequestFactory.CreateFile(Nonce, "sender", "Laptop", entry);
        var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, request, CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TransferAcknowledgement(true, 4), CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TransferAcknowledgement(true, 10), CancellationToken.None);
        stream.Position = 0;

        var store = new OneTimeAuthorizationStore();
        store.Register(Nonce, Now.AddMinutes(1), Now);
        var decoded = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(stream, CancellationToken.None);
        ProtocolSessionAuthorizer.ValidateAndAuthorize(decoded, Now, nonce => store.TryConsume(nonce, Now));

        var resume = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(stream, CancellationToken.None);
        Assert.Equal(4, TransferResponsePolicy.ValidateResumeOffset(resume.Accepted, resume.ResumeOffset, entry.Length, resume.Message));

        var completed = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(stream, CancellationToken.None);
        TransferResponsePolicy.ValidateCompletion(completed.Accepted, completed.ResumeOffset, entry.Length, completed.Message);
        Assert.Throws<UnauthorizedAccessException>(() =>
            ProtocolSessionAuthorizer.ValidateAndAuthorize(decoded, Now, nonce => store.TryConsume(nonce, Now)));
    }

    [Fact]
    public async Task BatchConversation_ValidatesSelectivePlanItemOrderAndFinalTotal()
    {
        var first = Entry("first.bin", 4, 'A');
        var second = Entry("second.bin", 6, 'B');
        var entries = new[] { first, second };
        var request = ProtocolRequestFactory.CreateBatch(Nonce, "sender", "Laptop", "batch-1", entries, 10);
        var plan = new BatchTransferResponse(
            true,
            [
                new BatchItemPlan(first.RelativePath, 1, true),
                new BatchItemPlan(second.RelativePath, 0, false, "Not selected")
            ]);

        var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, request, CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, plan, CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new BatchItemStart(first.RelativePath), CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TransferAcknowledgement(true, first.Length), CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TransferAcknowledgement(true, 10), CancellationToken.None);
        stream.Position = 0;

        var store = new OneTimeAuthorizationStore();
        store.Register(Nonce, Now.AddMinutes(1), Now);
        var decodedRequest = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(stream, CancellationToken.None);
        ProtocolSessionAuthorizer.ValidateAndAuthorize(decodedRequest, Now, nonce => store.TryConsume(nonce, Now));

        var decodedPlan = await FrameProtocol.ReadJsonAsync<BatchTransferResponse>(stream, CancellationToken.None);
        var validated = BatchTransferPlanValidator.Validate(entries, decodedPlan);
        Assert.True(validated[first.RelativePath].Accepted);
        Assert.False(validated[second.RelativePath].Accepted);

        var start = await FrameProtocol.ReadJsonAsync<BatchItemStart>(stream, CancellationToken.None);
        IncomingRequestPolicy.ValidateBatchItemStart(first.RelativePath, start.RelativePath);
        var itemAck = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(stream, CancellationToken.None);
        TransferResponsePolicy.ValidateCompletion(itemAck.Accepted, itemAck.ResumeOffset, first.Length, itemAck.Message);
        var finalAck = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(stream, CancellationToken.None);
        TransferResponsePolicy.ValidateCompletion(finalAck.Accepted, finalAck.ResumeOffset, 10, finalAck.Message);
    }

    [Fact]
    public async Task BatchConversation_RejectsReorderedItemStart()
    {
        var first = Entry("first.bin", 4, 'A');
        var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, new BatchItemStart("other.bin"), CancellationToken.None);
        stream.Position = 0;

        var start = await FrameProtocol.ReadJsonAsync<BatchItemStart>(stream, CancellationToken.None);
        Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateBatchItemStart(first.RelativePath, start.RelativePath));
    }

    [Fact]
    public async Task TextConversation_RoundTripsAndValidatesZeroOffsetAcknowledgement()
    {
        var request = ProtocolRequestFactory.CreateText(Nonce, "sender", "Phone", "hello", Now.AddMinutes(1), Now);
        var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, request, CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TransferAcknowledgement(true, 0), CancellationToken.None);
        stream.Position = 0;

        var store = new OneTimeAuthorizationStore();
        store.Register(Nonce, Now.AddMinutes(1), Now);
        var decoded = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(stream, CancellationToken.None);
        ProtocolSessionAuthorizer.ValidateAndAuthorize(decoded, Now, nonce => store.TryConsume(nonce, Now));
        var ack = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(stream, CancellationToken.None);
        TransferResponsePolicy.ValidateTextAcknowledgement(ack.Accepted, ack.ResumeOffset, ack.Message);
    }

    [Fact]
    public async Task PairConversation_DoesNotConsumeTransferAuthorization()
    {
        var request = ProtocolRequestFactory.CreatePairRequest("sender", "Phone", "12345678");
        var response = new PairingResponse(true, null, "swiftdrop://pair?p=example");
        var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, request, CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, response, CancellationToken.None);
        stream.Position = 0;

        var consumeCalls = 0;
        var decoded = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(stream, CancellationToken.None);
        ProtocolSessionAuthorizer.ValidateAndAuthorize(decoded, Now, _ =>
        {
            consumeCalls++;
            return true;
        });
        Assert.Equal(0, consumeCalls);

        var decodedResponse = await FrameProtocol.ReadJsonAsync<PairingResponse>(stream, CancellationToken.None);
        Assert.True(decodedResponse.Accepted);
        Assert.Equal(response.PairingLink, decodedResponse.PairingLink);
    }

    private static FileManifestEntry Entry(string path, long length, char hash)
        => new(path, length, new string(hash, 64), Now);
}
