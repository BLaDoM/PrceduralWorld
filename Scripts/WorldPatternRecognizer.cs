using UnityEngine;
using System.Collections.Generic;
using System;

public class WorldPatternRecognizer {

    public static Formation[] DetectNaturalFormations(float[,] map, float seaLevel, int maxIslandSize, int maxLakeSize) {

        float startTime = Time.realtimeSinceStartup;

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        List<Formation> formations = new List<Formation>();

        bool[,] landMap = new bool[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (map[x, y] >= seaLevel)
                    landMap[x, y] = true;

        List<int> links = new List<int>();

        int[,] labelMap = new int[width, height];

        int currentLabel = 1;
        links.Add(0);
        links.Add(0);

        //Set all labels
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (landMap[x, y]) {

                    int min = MinimumNonZero(labelMap[x - 1, y], labelMap[x - 1, y - 1], labelMap[x, y - 1], labelMap[x + 1, y - 1]);
                    int max = Mathf.Max(labelMap[x - 1, y], labelMap[x - 1, y - 1], labelMap[x, y - 1], labelMap[x + 1, y - 1]);

                    if (min == 0) {
                        //Add new label
                        currentLabel++;
                        labelMap[x, y] = currentLabel;
                        links.Add(0);
                    }
                    else {
                        labelMap[x, y] = min;
                        if (max > min)
                            links[max] = min;
                    }
                }

        //BackTrace label body
        for(int i = 0; i < links.Count; i++)
            if(links[i] != 0) {

            int c = i;
            int min = links[i];

            while (links.Contains(c)) {
                int t = links.IndexOf(c);
                c = links[t];
                links[t] = min;
            }

        }

        //Converge all labels
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (landMap[x, y])
                    if (links[labelMap[x, y]] != 0)
                        labelMap[x, y] = links[labelMap[x, y]];

        Dictionary<int, Point> maxValues = new Dictionary<int, Point>();
        Dictionary<int, Point> minValues = new Dictionary<int, Point>();

        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if(landMap[x, y] && labelMap[x, y] > 0) {

                    if (!maxValues.ContainsKey(labelMap[x, y])) {
                        maxValues.Add(labelMap[x, y], new Point(x, y));
                        minValues.Add(labelMap[x, y], new Point(x, y));
                    }

                    if (maxValues[labelMap[x, y]].x < x)
                        maxValues[labelMap[x, y]] = new Point(x, maxValues[labelMap[x, y]].y);

                    if (maxValues[labelMap[x, y]].y < y)
                        maxValues[labelMap[x, y]] = new Point(maxValues[labelMap[x, y]].x, y);

                    if (minValues[labelMap[x, y]].x > x)
                        minValues[labelMap[x, y]] = new Point(x, minValues[labelMap[x, y]].y);

                    if (minValues[labelMap[x, y]].y > y)
                        minValues[labelMap[x, y]] = new Point(minValues[labelMap[x, y]].x, y);

                }

        for(int i = 0; i < currentLabel; i++)
            if(maxValues.ContainsKey(i)){

            Formation f = new Formation();

            f.position = minValues[i];
            f.size = maxValues[i] - minValues[i];

            formations.Add(f);

        }

        Texture2D tex = new Texture2D(width, height);

        tex.filterMode = FilterMode.Point;

        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++) {

                tex.SetPixel(x, y, new Color(labelMap[x, y] * 0.00390625f * 50, (labelMap[x, y] / 10) * 0.00390625f * 50, (labelMap[x, y] / 100) * 0.00390625f * 50));

            }

        tex.Apply();

        foreach (Formation f in formations)
            f.GenerateName();

        GameObject.FindObjectOfType<WorldGenerator>().regions[5].texture = tex;

        Debug.Log(formations.Count + " formations detected");

        return formations.ToArray();

    }

    public static int MinimumNonZero(params int[] values) {

        int max = int.MaxValue;

        foreach (int i in values)
            if (i > 0 && i < max)
                max = i;

        if (max == int.MaxValue)
            return 0;
        else
            return max;

    }

    public static int[] MaximumsOverX(int x, params int[] values) {

        //Yeah, it's that specific, deal with it

        List<int> tmp = new List<int>();

        foreach (int t in values)
            if (t > x)
                tmp.Add(t);

        return tmp.ToArray();

    }

    static void GenerateDebugMap(bool[,] landMap) {

        int width = landMap.GetLength(0);
        int height = landMap.GetLength(1);

        Texture2D t = new Texture2D(width, height);

        t.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                t.SetPixel(x, y, landMap[x, y] ? Color.black : Color.white);

        t.Apply();

        GameObject.FindWithTag("GameController").GetComponent<MeshRenderer>().sharedMaterial.mainTexture = t;

    }

    static bool[,] DetectEdgesBinaryMap(bool[,] map) {

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        bool[,] ret = new bool[width, height];

        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (map[x, y] && map[x - 1, y] && map[x - 1, y - 1] && map[x, y - 1] && map[x + 1, y] &&
                    map[x + 1, y - 1] && map[x + 1, y + 1] && map[x - 1, y + 1] && map[x, y + 1])
                    ret[x, y] = true;

        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                ret[x, y] = !ret[x, y] && map[x, y];

        return ret;

    }

}

public struct Point : IEquatable<Point> {

    public int x;
    public int y;

    public Point(int x, int y) {

        this.x = x;
        this.y = y;

    }

    public static Point operator +(Point a, Point b) {

        return new Point(a.x + b.x, a.y + b.y);

    }

    public static Point operator -(Point a, Point b) {

        return new Point(a.x - b.x, a.y - b.y);

    }

    public bool Equals(Point t) {

        return (t.x == x && t.y == y);

    }

}

public class Formation {

    public string name;
    public Point position;
    public Point size;
    public bool[,] landMass;

    public bool[,] markLandMass(bool[,] previousMap) {

        for (int y = position.y; y < position.y + size.y; y++)
            for (int x = position.x; x < position.x + size.x; x++)
                previousMap[x, y] = previousMap[x, y] || landMass[x, y];

        return previousMap;

    }

    public void GenerateName() {



    }

}

public class Island : Formation {



}
