using System.Security.Cryptography;

namespace BIITPaperSettingAPI.Services;

public record StoredDiagram(string Key, string Name, string MimeType, string Checksum);

public class PrivateDiagrams(IConfiguration config)
{
    private readonly string folder = config["Files:PrivateRoot"] ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BIITPaperSetting", "PrivateDiagrams");

    // =====================================================
    // STEP 1: VALIDATE AND STORE THE DIAGRAM PRIVATELY
    // =====================================================
    // Original name sirf download ke liye hai. Disk path server khud banata hai.
    public async Task<StoredDiagram?> StoreAsync(IFormFile file, CancellationToken cancellation)
    {
        if (file.Length is <= 0 or > 5*1024*1024)
            return null;
        string name = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256 || name.Any(char.IsControl))
            return null;
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer,cancellation);
        byte[] bytes = buffer.ToArray();
        string extension = Path.GetExtension(name).ToLowerInvariant();
        string mime;
        if (extension == ".png" && bytes.Length >= 45 &&
            bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) &&
            bytes.AsSpan(12,4).SequenceEqual("IHDR"u8) && bytes.AsSpan(bytes.Length-8,4).SequenceEqual("IEND"u8))
            mime = "image/png";
        else if ((extension == ".jpg" || extension == ".jpeg") && bytes.Length >= 4 &&
            bytes[0]==255 && bytes[1]==216 && bytes[2]==255 && bytes[^2]==255 && bytes[^1]==217)
            mime = "image/jpeg";
        else
            return null;

        string key = await StoreBytesAsync(bytes,cancellation);
        return new StoredDiagram(key,name,mime,Convert.ToHexString(SHA256.HashData(bytes)));
    }

    // Generated PDF bhi configured private folder mein hi rehti hai.
    public async Task<string> StoreBytesAsync(byte[] bytes, CancellationToken cancellation)
    {
        string key = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(folder);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(folder,key),bytes,cancellation);
            return key;
        }
        catch
        {
            Delete(key);
            throw;
        }
    }
    public FileStream? Open(string key)
    {
        if (!Guid.TryParseExact(key,"N",out _))
            return null;
        string path = Path.Combine(folder,key);
        if (!File.Exists(path))
            return null;
        return File.OpenRead(path);
    }

    public void Delete(string generatedKey) => File.Delete(Path.Combine(folder,generatedKey));
}
