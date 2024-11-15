﻿using ImageMagick;
using System.Text;

namespace Converter;

public static class Helper
{
    // Generates path that works in both Windows and Mac
    public static string GetPath(params string[] paths)
    {
        if (paths is null) return "";
        try
        {
            return Path.Combine(paths.SelectMany(n => n.Replace(@"\\", "/").Replace(@"\", "/").Split("/")).ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to combine paths: {string.Join(",", paths)}", ex);
        }
    }

    public static bool IsCellHighMountains(int height)
    {
        return height > 2000;
    }
    public static bool IsCellMountains(int height)
    {
        return height is > 1000 and <= 2000;
    }
    public static bool IsCellLowMountains(int height)
    {
        return height is > 500 and <= 1000;
    }
    public static bool IsCellHills(int biomeId, int height)
    {
        if (IsCellHighMountains(height) || IsCellMountains(height) || IsCellLowMountains(height))
        {
            return false;
        }
        return IsHills(biomeId, height);
    }
    private static bool IsHills(int biomeId, int heightDifference)
    {
        return heightDifference > 200 && biomeId is 4 or 5 or 6 or 7 or 8;
    }
    public static string MapBiome(int biomeId)
    {
        return biomeId switch
        {
            1 => "desert",// Hot desert > desert
            2 => "taiga",// Cold desert > taiga
            3 => "steppe",// Savanna > steppe
            4 => "plains",// Grassland > plains
            5 => "farmlands",// Tropical seasonal forest > farmlands
            6 => "forest",// Temperate deciduous forest > forest
            7 => "jungle",// Tropical rainforest > jungle
            8 => "forest",// "Temperate rainforest" > forest
            9 => "taiga",// Taiga > taiga
            10 => "taiga",// Tundra > taiga
            11 => "drylands",// Glacier > floodplains
            12 => "floodplains",// Wetland > wetlands
            _ => throw new ArgumentException("Unrecognized biomeId")
        };
    }
    public static string? GetProvinceBiomeName(int biomeId, int heightDifference)
    {
        // Marine > ocean
        if (biomeId == 0)
            return null;

        // plains/farmlands/hills/mountains/desert/desert_mountains/oasis/jungle/forest/taiga/wetlands/steppe/floodplains/drylands
        /*
         * 	0"Marine",
			1"Hot desert",
			2"Cold desert",
			3"Savanna",
			4"Grassland",
			5"Tropical seasonal forest",
			6"Temperate deciduous forest",
			7"Tropical rainforest",
			8"Temperate rainforest",
			9"Taiga",
			10"Tundra",
			11"Glacier",
			12"Wetland"
         * */
        if (IsCellHighMountains(heightDifference))
        {
            return biomeId switch
            {
                1 or 3 => "desert_mountains",
                _ => "mountains",
            };
        }
        else if (IsCellMountains(heightDifference))
        {
            return biomeId switch
            {
                1 => "drylands",
                2 => "taiga",
                3 => "drylands",
                4 => "hills",
                5 or 6 or 7 or 8 => "taiga",
                9 or 10 or 11 => "mountains",
                12 => "farmlands",
                _ => throw new ArgumentException("Unrecognized biomeId")
            };
        }
        else if (IsHills(biomeId, heightDifference))
        {
            return biomeId switch
            {
                1 => "oasis",
                2 => "steppe",
                3 => "hills",
                4 => "hills",
                5 or 6 or 7 or 8 => "taiga",
                9 or 10 or 11 => "drylands",
                12 => "wetlands",
                _ => throw new ArgumentException("Unrecognized biomeId")
            };
        }
        return MapBiome(biomeId);
    }

    //public static double Percentile(double[] sequence, double excelPercentile)
    //{
    //    Array.Sort(sequence);
    //    int N = sequence.Length;
    //    double n = (N - 1) * excelPercentile + 1;
    //    // Another method: double n = (N + 1) * excelPercentile;
    //    if (n == 1d) return sequence[0];
    //    else if (n == N) return sequence[N - 1];
    //    else
    //    {
    //        int k = (int)n;
    //        double d = n - k;
    //        return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
    //    }
    //}
    public static double Percentile(int[] sequence, double excelPercentile)
    {
        Array.Sort(sequence);
        int N = sequence.Length;
        double n = (N - 1) * excelPercentile + 1;
        // Another method: double n = (N + 1) * excelPercentile;
        if (n == 1d) return sequence[0];
        else if (n == N) return sequence[N - 1];
        else
        {
            int k = (int)n;
            double d = n - k;
            return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
        }
    }
    public static double Percentile(float[] sequence, double excelPercentile)
    {
        Array.Sort(sequence);
        int N = sequence.Length;
        double n = (N - 1) * excelPercentile + 1;
        // Another method: double n = (N + 1) * excelPercentile;
        if (n == 1d) return sequence[0];
        else if (n == N) return sequence[N - 1];
        else
        {
            int k = (int)n;
            double d = n - k;
            return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
        }
    }

    public static double HeightDifference(Province province)
    {
        var heights = province.Cells.Select(n => n.height).ToArray();
        return Percentile(heights, 0.7) - Percentile(heights, 0.3);
    }

    public static PointD GeoToPixel(float lon, float lat, Map map)
    {
        return new PointD((lon - map.XOffset) * map.XRatio, Map.MapHeight - (lat - map.YOffset) * map.YRatio);
    }
    public static PointD GeoToPixelCrutch(float lon, float lat, Map map)
    {
        return new PointD((lon - map.XOffset) * map.XRatio, (lat - map.YOffset) * map.YRatio);
    }
    public static PointD PixelToFullPixel(float x, float y, Map map)
    {
        return new PointD(x * map.pixelXRatio, Map.MapHeight - y * map.pixelYRatio);
    }

    /// <summary>
    /// Examples:
    /// 1. await WriteLocalizationFile(map, "dynasties", "dynasty_names_l_", content)
    ///    localization\{language}\dynasties\{filePrefix}{language}.yml
    /// 2. await WriteLocalizationFile(map, null, "dynasty_names_l_", content)
    ///    localization\{language}\{filePrefix}{language}.yml
    /// </summary>
    /// <param name="localizationPath">pass null if in localization root</param>
    /// <param name="filePrefix">dynasty_names_l_</param>
    public static async Task WriteLocalizationFile(Map map, string? localizationPath, string filePrefix, string content, string lastHeaderLineContains)
    {
        var languages = new string[] { "english", "french", "german", "korean", "russian", "simp_chinese", "spanish" };

        foreach (var language in languages)
        {
            var fileName = $"{filePrefix}{language}.yml";

            var originalFilePath = localizationPath is null
                ? GetPath(map.Settings.Ck3Directory, "localization", language, fileName)
                : GetPath(map.Settings.Ck3Directory, "localization", language, localizationPath, fileName);

            var header = File.ReadLines(originalFilePath).TakeWhile(n => !n.Contains(lastHeaderLineContains));
            var file = $"{string.Join("\n", header)}\n{content}";

            var outputPath = localizationPath is null
                ? GetPath(Settings.OutputDirectory, "localization", language, fileName)
                : GetPath(Settings.OutputDirectory, "localization", language, localizationPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            await File.WriteAllTextAsync(outputPath, file, new UTF8Encoding(true));
        }
    }
}
