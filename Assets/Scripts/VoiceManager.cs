using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows.Speech;
using UnityEngine.SceneManagement;
public class VoiceManager : MonoBehaviour
{
    static KeywordRecognizer key;
    public static Dictionary<string, System.Action> words;
    float prevTime = 1f;

    private void Awake()
    {
        words = new Dictionary<string, System.Action>();
        words.Add("working", () => Debug.Log("yes"));
        words.Add("pause", () =>
        {
            prevTime = (Time.timeScale == 0 ? prevTime : Time.timeScale); 
            Time.timeScale = 0;
        });
        words.Add("play", () => Time.timeScale = prevTime);
        words.Add("skip", SkipScene);
        key = new KeywordRecognizer(words.Keys.ToArray());
        key.OnPhraseRecognized += (ctx) => words[ctx.text].Invoke();
        key.Start();
    }

    private void SkipScene()
    {
        int ind = SceneManager.GetActiveScene().buildIndex;
        if(ind == 4)
        {
            Application.Quit();
        }
        else
        {
            SceneManager.LoadScene(ind + 1);
        }
    }

    public static void RemoveWord(string word)
    {
        words.Remove(word);
        key.Stop();
        key.Dispose();
        key = new KeywordRecognizer (words.Keys.ToArray());
        key.OnPhraseRecognized += (ctx) => words[ctx.text].Invoke();
        key.Start();
    }

    public static void AddWord(string word, System.Action act)
    {
        words.Add(word, act);
        key.Stop();
        key.Dispose();
        key = new KeywordRecognizer(words.Keys.ToArray());
        key.OnPhraseRecognized += (ctx) => words[ctx.text].Invoke();
        key.Start();
    }

    private void OnDestroy()
    {
        key.Stop();
        key.Dispose();
    }
}