using UnityEngine;
using System.Collections.Generic;

public class WorldPatternRecognizer {

    public static void DetectNaturalFormations(float[,] map, float seaLevel, int maxIslandSize, int maxLakeSize) {

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        List<Formation> formations = new List<Formation>();

        bool[,] landMap = new bool[width, height];

        bool[,] evaluatedMap = new bool[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (map[x, y] >= seaLevel)
                    landMap[x, y] = true;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!evaluatedMap[x, y])
                    if (landMap[x, y]) {

                        Formation i = DetectIslandSize(landMap, x, y);
                        i.markLandMass(evaluatedMap);
                        formations.Add(i);

                    }

        Debug.Log(formations.Count);

    }

    static Formation DetectIslandSize(bool[,] landMap, int x, int y) {

        int width = landMap.GetLength(0);
        int height = landMap.GetLength(1);

        bool[,] wasChecked = new bool[width, height];

        bool[,] landMass = new bool[width, height];
        List<Point> toBeChecked = new List<Point>();

        toBeChecked.Add(new Point(x, y));

        int maxY = int.MinValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int minX = int.MaxValue;

        while (toBeChecked.Count > 0) {

            List<Point> toBeAdded = new List<Point>();
            Point lastIterated = new Point(-1, -1);

            foreach (Point p in toBeChecked) {

                if (landMap[p.x, p.y + 1])
                    if (!wasChecked[p.x, p.y + 1]) {
                        toBeAdded.Add(new Point(p.x, p.y + 1));
                        wasChecked[p.x, p.y + 1] = true;
                        if (p.y > maxY)
                            maxY = p.y;
                        else
                            if (p.y < minY)
                            minY = p.y;
                    }

                if (landMap[p.x, p.y - 1])
                    if (!wasChecked[p.x, p.y - 1]) {
                        toBeAdded.Add(new Point(p.x, p.y - 1));
                        wasChecked[p.x, p.y - 1] = true;
                        if (p.y > maxY)
                            maxY = p.y;
                        else
                            if (p.y < minY)
                            minY = p.y;
                    }

                if (landMap[p.x + 1, p.y])
                    if (!wasChecked[p.x + 1, p.y]) {
                        toBeAdded.Add(new Point(p.x + 1, p.y));
                        wasChecked[p.x + 1, p.y] = true;
                        if (p.x > maxX)
                            maxX = p.x;
                        else
                            if (p.x < minX)
                            minX = p.x;
                    }

                if (landMap[p.x - 1, p.y])
                    if (!wasChecked[p.x - 1, p.y]) {
                        toBeAdded.Add(new Point(p.x - 1, p.y));
                        wasChecked[p.x - 1, p.y] = true;
                        if (p.x > maxX)
                            maxX = p.x;
                        else
                            if (p.x < minX)
                            minX = p.x;
                    }

                lastIterated = p;
                landMass[p.x, p.y] = true;

            }

            toBeChecked.Remove(lastIterated);
            toBeChecked.AddRange(toBeAdded);

        }

        Formation formation = new Formation();

        formation.position = new Point(minX, minY);
        formation.size = new Point(maxX - minX, maxY - minY);

        formation.landMass = landMass;

        return formation;

    }

    public struct Point {

        public int x;
        public int y;

        public Point(int x, int y) {

            this.x = x;
            this.y = y;

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
                    if (landMass[x, y])
                        previousMap[x, y] = true;

            return previousMap;

        }

    }

    public class Island : Formation {

        

    }

}
