using System;
using System.IO;
using System.Text.Json;

namespace Companion.Asset;

public sealed class PetAssetProfileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public PetAssetProfile LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Pet profile not found: {path}");
        }

        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<PetAssetProfile>(json, JsonOptions)
                      ?? throw new InvalidDataException($"Invalid profile JSON: {path}");

        PetAssetProfileValidator.Validate(profile);
        return profile;
    }
}
