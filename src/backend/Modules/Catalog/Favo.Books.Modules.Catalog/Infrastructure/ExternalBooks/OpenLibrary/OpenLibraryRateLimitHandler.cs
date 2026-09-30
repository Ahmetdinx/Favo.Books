namespace Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryRateLimitHandler(
    OpenLibraryRequestGate requestGate)
    : DelegatingHandler
{
    private readonly OpenLibraryRequestGate
        _requestGate = requestGate;

    protected override async Task<HttpResponseMessage>
        SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
    {
        await _requestGate.WaitAsync(
            cancellationToken);

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}