using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour {

    public Octave[] octaves;
    public int width;
    public int height;
    public float scale;

    public int seed;

    public float realScale;

    public bool autoUpdate;

    public bool invert;
    public int falloffMargin;

    public Textures[] regions;

    public int regionDisplay;

    public GameObject go;

    public Texture2D overlay;

    public void Start() {

        //overlay.Resize(width, height, TextureFormat.RGBA32, true);

        float startTime = Time.realtimeSinceStartup;

        System.Random rng = new System.Random(seed);

        for (int i = 0; i < octaves.Length; i++)
            octaves[i].offset = new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000));

        float[,] map = GenerateNoiseMap(width, height, scale, octaves, invert, falloffMargin);

        for (int i = 0; i < regions.Length; i++)
            regions[i].texture = GenerateTexture(map, regions[i].regions);

        go.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = regions[regionDisplay].texture;
        go.transform.localScale = new Vector3(width, 0, height) * realScale;

        Debug.Log("Finished in " + (Time.realtimeSinceStartup - startTime));

        //WorldPatternRecognizer.DetectNaturalFormations(map, 0.5f, 100, 100);

    }

    public void SetDisplayTexture() {

        go.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = regions[regionDisplay].texture;

    }

    public void SortRegions(int region) {

        List<Region> reg = new List<Region>(regions[region].regions);

        reg.Sort((a, b) => Mathf.RoundToInt(a.height * 100 - b.height * 100));

        regions[region].regions = reg.ToArray();

    }

    public static float[,] GenerateNoiseMap(int width, int height, float scale, Octave[] octaves, bool invert, int falloff) {

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
                    map[x, y] = Mathf.InverseLerp(max, min, map[x, y]) * FalloffCoord(x, y, width, height, falloff);
                else
                    map[x, y] = Mathf.InverseLerp(min, max, map[x, y]) * FalloffCoord(x, y, width, height, falloff);

        return map;

    }

    static float FalloffCoord(int x, int y, int width, int height, int margin) {

        int halfWidth = width / margin;
        int halfHeight = height / margin;

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

}

public class Formation {

    public string name;

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

    public string name;
    public Color color;
    public float height;

}
