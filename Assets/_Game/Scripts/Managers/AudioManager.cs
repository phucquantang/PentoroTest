using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct NamedAudioClip
{
    public string Id;
    public AudioClip Clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource _sfxSource;

    [SerializeField] private List<NamedAudioClip> _audioClips;

    private Dictionary<string, AudioClip> _audioClipDict;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioClipDict = new Dictionary<string, AudioClip>();
        foreach (var entry in _audioClips)
        {
            _audioClipDict[entry.Id] = entry.Clip;
        }

    }

    public void PlaySFX(string id, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (!_audioClipDict.TryGetValue(id, out var clip)) return;

        var randomPitch = Random.Range(minPitch, maxPitch);
        _sfxSource.pitch = randomPitch;
        _sfxSource.PlayOneShot(clip, volume);

        _sfxSource.pitch = 1f;
    }
}
