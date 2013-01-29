namespace JabbR.Services
{
    public interface IKeyProvider
    {
        byte[] EncryptionKey { get; }
        byte[] ValidationKey { get; }
    }
}