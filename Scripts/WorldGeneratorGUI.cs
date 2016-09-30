using UnityEngine;
using UnityEngine.Windows;
using System.Collections;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorGUI : Editor {

    public override void OnInspectorGUI() {

        WorldGenerator w = FindObjectOfType<WorldGenerator>();

        if (DrawDefaultInspector())
            if (w.autoUpdate)
                w.Start();

        if(GUILayout.Button("Generate"))
            w.Start();

        if (GUILayout.Button("Randomize Seed"))
            w.seed = Random.Range(0, int.MaxValue);

        if (GUILayout.Button("Sort Regions"))
            for(int i = 0; i < w.regions.Length; i++)
                w.SortRegions(i);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Previous Texture")) {
            if (w.regionDisplay <= 0)
                w.regionDisplay = w.regions.Length - 1;
            else
                w.regionDisplay--;

            w.SetDisplayTexture();

        }

        if (GUILayout.Button("Next Texture")) {
            if (w.regionDisplay >= w.regions.Length - 1)
                w.regionDisplay = 0;
            else
                w.regionDisplay++;

            w.SetDisplayTexture();

        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Export PNG")) {

            Texture2D r;

            if (w.overlay != null) {
                r = new Texture2D(w.width, w.height);

                r.wrapMode = TextureWrapMode.Clamp;

                for (int x = 0; x < w.width; x++)
                    for (int y = 0; y < w.height; y++)
                        r.SetPixel(x, y, w.regions[w.regionDisplay].texture.GetPixel(x, y) + w.overlay.GetPixel(x, y));

                r.Apply();

            }
            else
                r = w.regions[w.regionDisplay].texture;

            byte[] b = r.EncodeToPNG();
            File.Create(Application.dataPath + "/Maps/" + w.seed + ".png").Dispose();
            File.WriteAllBytes(Application.dataPath + "/Maps/" + w.seed + ".png", b);

        }

    }

}
