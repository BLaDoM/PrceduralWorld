using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class NameGenerator : MonoBehaviour {

    public WordGroup words;

    public string file;

    public void Start() {

        ReadFile(file);

    }

    public static void ReadFile(string file) {

        StreamReader reader = new StreamReader(Application.dataPath + file);

        WordGroup wordG = new WordGroup();

        wordG.name = reader.ReadLine();
        List<string>[] newWords = new List<string>[int.Parse(reader.ReadLine()) + 1];

        string s = "";

        wordG.words = new Word[newWords.Length - 1];

        for (int i = 0; i < newWords.Length; i++) {
            newWords[i] = new List<string>();

            while (!reader.EndOfStream) {
                s = reader.ReadLine();

                if (s[0] == '*') {
                    wordG.words[i].name = s.Remove(0, 1);//s.Remove(0, 1);
                    break;
                }

                newWords[i].Add(s);

            }
        }

        for (int i = 0; i < newWords.Length - 1; i++)
            wordG.words[i].words = newWords[i + 1].ToArray();

        reader.Close();

        FindObjectOfType<NameGenerator>().words = wordG;

    }

    public string Generate(System.Random rng, params string[] args) {

        string ret = "";

        List<string> par = new List<string>(args);

        List<string> prefixes = new List<string>();
        List<string> midfixes = new List<string>();
        List<string> sufixes = new List<string>();

        for (int i = 0; i < words.words.Length; i++)
            if(par.Contains(words.words[i].name) || par.Contains("All"))
            for(int j = 0; j < words.words[i].words.Length; j++)
                switch (words.words[i].words[j][0]) {

                    case '+': sufixes.Add(words.words[i].words[j].Remove(0, 1)); break;
                    case '-': prefixes.Add(words.words[i].words[j].Remove(0, 1)); break;
                    case '=': midfixes.Add(words.words[i].words[j].Remove(0, 1)); break;

                }

        ret = string.Concat(ret, prefixes[rng.Next(0, prefixes.Count)]);

        if(midfixes.Count > 0 && rng.Next(0, prefixes.Count - midfixes.Count) == 0)
            ret = string.Concat(ret, midfixes[rng.Next(0, midfixes.Count)]);

        ret = string.Concat(ret, sufixes[rng.Next(0, sufixes.Count)]);

        ret = char.ToUpper(ret[0]) + ret.Substring(1);

        return ret;

    }

}

[System.Serializable]
public struct WordGroup {

    //Things like hot, cold, high, tall, glorious, rich, poor, etc.
    public string name;
    public Word[] words;

}

[System.Serializable]
public struct Word {

    public string name;
    public string[] words;

}