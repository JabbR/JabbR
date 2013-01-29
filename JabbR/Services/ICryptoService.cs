
namespace JabbR.Services
{
    public interface ICryptoService
    {
        byte[] Protect(byte[] plainText);
        byte[] Unprotect(byte[] payload);
        string CreateSalt();
    }
}