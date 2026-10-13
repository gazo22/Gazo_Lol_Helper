using System.Security.Cryptography;
using System.Text;

namespace RiftLens.UI.Services;

/// <summary>
/// Cache disque des images Data Dragon. Les URLs contiennent la version du patch : un fichier téléchargé
/// ne change jamais, il est donc servi indéfiniment depuis le disque.
/// </summary>
public sealed class DiskAssetCache(HttpClient http, string directory)
{
    public const string Host = "ddragon.leagueoflegends.com";

    public static bool IsCacheable(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.Contains("/img/", StringComparison.Ordinal);

    /// <summary>Chemin local de l'image (téléchargée si absente), ou null si non cacheable / indisponible.</summary>
    public async Task<string?> GetAsync(Uri uri, CancellationToken ct = default)
    {
        if (!IsCacheable(uri))
        {
            return null;
        }

        var path = PathFor(uri);
        if (File.Exists(path))
        {
            return path;
        }

        try
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var file = File.Create(temp))
            {
                await response.Content.CopyToAsync(file, ct);
            }

            File.Move(temp, path, overwrite: true); // écriture atomique : jamais de fichier tronqué servi
            return path;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "image/png",
    };

    internal string PathFor(Uri uri)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsolutePath))).ToLowerInvariant();
        var extension = Path.GetExtension(uri.AbsolutePath);
        return Path.Combine(directory, hash[..2], hash + (extension.Length is > 0 and <= 5 ? extension : ".png"));
    }
}
