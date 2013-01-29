namespace JabbR.Services
{
    public interface IKeyProvider
    {
        byte[] EncryptionKey { get; }
        byte[] VerificationKey { get; }
    }
}