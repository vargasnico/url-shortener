using System.Security.Cryptography;

namespace UrlShortener.Api.Services;

public class RandomCodeGenerator : ICodeGenerator
{
    private const string Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public const int Length = 6;

    // RandomNumberGenerator em vez de Random: códigos imprevisíveis,
    // para ninguém conseguir adivinhar os links dos outros.
    public string Generate() => RandomNumberGenerator.GetString(Characters, Length);
}
