using UnityEngine;

public class NoteGenerator : MonoBehaviour
{
    const int sampleRate = 44100;
    [SerializeField] float clipDuration = 1.0f;
    float[] frequencies = new float[] { 440.0f, 493.88f, 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880.0f };
    AudioClip[] notes = new AudioClip[5];
    [SerializeField] AudioSource audioSource;

    void Start()
    {
        for (int i = 0; i < 5; i++) 
        {
            notes[i] = GenerateNoteClip(frequencies[i]);
        }
    }

    AudioClip GenerateNoteClip(float frequency)
    {
        int numSamples = (int)(sampleRate * clipDuration);
        float[] samples = new float[numSamples];

        //angular frequency (2 * pi * frequency)
        float angularFrequency = 2 * Mathf.PI * frequency;

        //sample
        for (int i = 0; i < numSamples; i++)
        {
            float time = i / (float)sampleRate;
            float sample = Mathf.Sin(angularFrequency * time);
            samples[i] = sample;
        }

        AudioClip clip = AudioClip.Create("NoteClip", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void PlayNote(int ind)
    {
        audioSource.PlayOneShot(notes[ind]);
    }
}
