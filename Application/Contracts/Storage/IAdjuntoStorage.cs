namespace Application.Contracts.Storage
{
    public interface IAdjuntoStorage
    {
        Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken);
    }
}
