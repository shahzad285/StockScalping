namespace StockTrading.IServices;

public interface IStringEncryptionService
{
    string Encrypt(string value);
    string Decrypt(string encryptedValue);
}
