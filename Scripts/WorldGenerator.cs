using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour {

    public bool debugEnabled;

    public Octave[] shapeOctaves;
    public Octave heatOctave;
    public Octave humidityOctave;

    public float heatMargin;

    public float shapeToHeightInterpolator;

    public int width;
    public int height;
    public float scale;

    public int seed;

    public float realScale;

    public bool autoUpdate;

    public bool invert;

    public float falloffMarginX, falloffMarginY;

    public float seaLevel;
    public float windDescentSpeed;
    public float humidityMax;

    public Textures[] regions;

    public int regionDisplay;

    public GameObject go;

    public Texture2D overlay;

    Formation[] formations;

    public enum biome {

        Ocean, Tundra, BorealForest, TemperateForest, Grassland, Savanna, TropicalForest, ColdDesert, Desert

    }

    public void Start() {

        //overlay.Resize(width, height, TextureFormat.RGBA32, true);

        float startTime = Time.realtimeSinceStartup;

        System.Random rng = new System.Random(seed);

        for (int i = 0; i < shapeOctaves.Length; i++)
            shapeOctaves[i].offset = new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000));

        humidityOctave.offset = new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000));
        heatOctave.offset = new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000));

        float[,] map = GenerateNoiseMap(width, height, scale, shapeOctaves, invert, falloffMarginX, falloffMarginY);
        float[,] heatMap = GenerateHeatMap(map, heatOctave, scale, false, heatMargin, seaLevel);
        float[,] humidityMap = GenerateHumidityMap(map, heatMap, humidityOctave, windDescentSpeed, seaLevel, humidityMax);

        biome[,] biomeMap = GenerateBiomeMap(heatMap, humidityMap);

        for (int i = 0; i < regions.Length - 3; i++)
            regions[i].texture = GenerateTexture(map, regions[i].regions);

        regions[regions.Length - 3].texture = GenerateTexture(heatMap, regions[regions.Length - 3].regions); //Heat Map Texture
        regions[regions.Length - 2].texture = GenerateTexture(humidityMap, regions[regions.Length - 2].regions); //Humidity Map Texture
        regions[regions.Length - 1].texture = WorldToTexture(biomeMap, map, regions[regions.Length - 1].regions, seaLevel); //World Map Texture

        go.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = regions[regionDisplay].texture;
        go.transform.localScale = new Vector3(width, -1, height) * -realScale;

        Debug.Log("Generation finished in " + (Time.realtimeSinceStartup - startTime));

        formations = WorldPatternRecognizer.DetectNaturalFormations(map, 0.5f, 100, 100);

        Debug.Log("Recognition finished in " + (Time.realtimeSinceStartup - startTime));

        foreach (Formation f in formations)
            Debug.Log(f.name);

    }

    public static float[,] GenerateNoiseMap(int width, int height, float scale, Octave[] octaves, bool invert, float falloffX, float falloffY) {

        float[,] map = new float[width, height];

        int midX = width / 2;
        int midY = height / 2;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                for (int i = 0; i < octaves.Length; i++) {

                    float perlinX = (octaves[i].offset.x + x) * octaves[i].size * scale * octaves[i].frequency;
                    float perlinY = (octaves[i].offset.y + y) * octaves[i].size * scale * octaves[i].frequency;

                    map[x, y] += Mathf.PerlinNoise(perlinX, perlinY) * octaves[i].amplitude;

                }

        float max = float.MinValue;
        float min = float.MaxValue;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (map[x, y] > max)
                    max = map[x, y];
                else
                    if (map[x, y] < min)
                    min = map[x, y];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (invert)
                    map[x, y] = Mathf.InverseLerp(max, min, map[x, y]) * FalloffCoord(x, y, width, height, falloffX, falloffY);
                else
                    map[x, y] = Mathf.InverseLerp(min, max, map[x, y]) * FalloffCoord(x, y, width, height, falloffX, falloffY);

        return map;

    }

    public static float[,] GenerateHeatMap(float[,] sMap, Octave octave, float scale, bool invert, float falloffY, float seaLevel) {

        int width = sMap.GetLength(0);
        int height = sMap.GetLength(1);

        float[,] map = new float[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                    map[x, y] = FalloffCoord(x, y, width, height, 0, falloffY) +
                    Mathf.PerlinNoise((octave.offset.x + x) * octave.frequency * octave.size,
                    (octave.offset.y + y) * octave.frequency * octave.size) * octave.amplitude;

        float minus = seaLevel * 1.175f;
         
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (sMap[x, y] > seaLevel)
                    map[x, y] -= Mathf.Max(sMap[x, y] - minus, 0);

        float max = float.MinValue;
        float min = float.MaxValue;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (map[x, y] > max)
                    max = map[x, y];
                else
                    if (map[x, y] < min)
                    min = map[x, y];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[x, y] = Mathf.InverseLerp(min, max, map[x, y]);

        return map;

    }

    public static float[,] GenerateHumidityMap(float[,] map, float[,] heatMap, Octave octave, float windDescentSpeed, float seaLevel, float relativeTo) {

        //All winds go east for fuckall reason

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        float[,] humidity = new float[width, height];

        //Shoot a "ray of wind" from west to east

        for (int y = 0; y < height; y++) {

            float currentHeight = Mathf.Max(map[width - 1, y], seaLevel);

            for (int x = width - 1; x >= 0; x--) {

                currentHeight = Mathf.Max(currentHeight - windDescentSpeed, map[x, y], seaLevel);

                if (map[x, y] > seaLevel)
                    humidity[x, y] = relativeTo - ((Mathf.PerlinNoise((octave.offset.x + x) * octave.frequency * octave.size,
                        (octave.offset.y + y) * octave.frequency * octave.size) * octave.amplitude) +
                        (currentHeight - Mathf.Max(map[x, y], seaLevel))); //I know what i'm doing, shut up
                else
                    humidity[x, y] = 2;

            }
        }

        return humidity;

    }

    public static biome[,] GenerateBiomeMap(float[,] heatMap, float[,] humidityMap) {

        int width = heatMap.GetLength(0);
        int height = heatMap.GetLength(1);

        biome[,] ret = new biome[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) {

                if (humidityMap[x, y] > 1)
                    ret[x, y] = biome.Ocean;
                else
                if (heatMap[x, y] <= 0.25f)
                    ret[x, y] = biome.Tundra;
                else
                    if (heatMap[x, y] <= 0.5f)
                    if (humidityMap[x, y] <= 0.5f)
                        ret[x, y] = biome.ColdDesert;
                    else
                        ret[x, y] = biome.BorealForest;
                else
                    if (heatMap[x, y] <= 0.75f)
                    if (humidityMap[x, y] <= 0.3f)
                        ret[x, y] = biome.Savanna;
                    else
                        if (humidityMap[x, y] <= 0.6f)
                        ret[x, y] = biome.Grassland;
                    else
                        ret[x, y] = biome.TemperateForest;
                else
                    if (humidityMap[x, y] <= 0.5f)
                    ret[x, y] = biome.Desert;
                else
                    ret[x, y] = biome.TropicalForest;

            }

        return ret;

    }

    public static Texture2D WorldToTexture(biome[,] biomeMap, float[,] map, Region[] region, float seaLevel) {

        int width = biomeMap.GetLength(0);
        int height = biomeMap.GetLength(1);

        Texture2D tex = new Texture2D(width, height);

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, Color.Lerp(region[(int)biomeMap[x, y]].color, Color.black, map[x, y] - seaLevel));

        tex.Apply();

        return tex;

    }

    float[,] MinimalizeDepthDifference(float[,] map, float minDepth, float maxHeight) {

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[x, y] = Mathf.InverseLerp(minDepth, maxHeight, map[x, y]);

        return map;

    }

    public void SetDisplayTexture() {

        go.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = regions[regionDisplay].texture;

    }

    public void SortRegions(int region) {

        List<Region> reg = new List<Region>(regions[region].regions);

        reg.Sort((a, b) => Mathf.RoundToInt(a.height * 100 - b.height * 100));

        regions[region].regions = reg.ToArray();

    }

    static float FalloffCoord(int x, int y, int width, int height, float marginX, float marginY) {

        int halfWidth;
        int halfHeight;

        if (marginX <= 0)
            halfWidth = 0;
        else
            halfWidth = Mathf.RoundToInt(width / marginX);

        if (marginY <= 0)
            halfHeight = 0;
        else
            halfHeight = Mathf.RoundToInt(height / marginY);

        float xF = 1;
        float yF = 1;

        if (x < halfWidth)
            xF = Mathf.InverseLerp(0, halfWidth, x);
        else
            if (x > width - halfWidth)
            xF = Mathf.InverseLerp(width, width - halfWidth, x);

        if (y < halfHeight)
            yF = Mathf.InverseLerp(0, halfHeight, y);
        else
            if (y > height - halfHeight)
            yF = Mathf.InverseLerp(height, height - halfHeight, y);

        return Mathf.Min(xF, yF);

    }

    public static Texture2D GenerateTexture(float[,] noiseMap, Region[] regions) {

        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D tex = new Texture2D(width, height);

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] colors = new Color[width * height];

        for(int y = 0; y < height; y++)
            for(int x = 0; x < width; x++)
                for(int i = 0; i < regions.Length; i++)
                    if(noiseMap[x, y] <= regions[i].height) {

                        colors[y * width + x] = regions[i].color;
                        break;

                    }

        tex.SetPixels(colors);

        tex.Apply();

        return tex;

    }

    public static Texture2D GenerateFinalTexture(float[,] noiseMap, Region[] regions) {

        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D tex = new Texture2D(width, height);

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                for (int i = 0; i < regions.Length; i++)
                    if (noiseMap[x, y] <= regions[i].height) {

                        colors[y * width + x] = regions[i].color;
                        break;

                    }

        tex.SetPixels(colors);

        tex.Apply();

        return tex;

    }

    public void OnDrawGizmos() {

        if (debugEnabled) {
            Gizmos.color = Color.red;

            if (formations != null)
                foreach (Formation f in formations)
                    Gizmos.DrawWireCube(new Vector3(f.position.x + f.size.x / 2, 0, f.position.y + f.size.y / 2) - new Vector3(width, 0, height) / 2,
                        new Vector3(f.size.x + 1, 1, f.size.y + 1));

        }
    }

}

[System.Serializable]
public struct Textures {

    public string name;
    public Region[] regions;
    public Texture2D texture;

}

[System.Serializable]
public struct Octave {

    public Vector2 offset;

    public float amplitude;
    public float frequency;
    public float size;

    public Octave(Vector2 _offset, float _amplitude, int _frequency, float _size) {

        offset = _offset;
        amplitude = _amplitude;
        frequency = _frequency;
        size = _size;

    }

}

[System.Serializable]
public struct Region {

    public Color color;
    public float height;

}
