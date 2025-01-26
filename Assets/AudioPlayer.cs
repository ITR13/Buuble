using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class AudioPlayer : MonoBehaviour
{
    private ObjectPool<AudioSource> _audioPool;

    [SerializeField] private AudioSource _prefab;
    [SerializeField] private AudioClip[] _audioClips;

    [SerializeField] private AudioClip _unpop;

    private Queue<AudioSource> _audioQueue = new();


    [SerializeField] private float[] weights;

    private void Awake()
    {
        _audioPool = new ObjectPool<AudioSource>(
            () => Instantiate(_prefab, transform),
            ac => ac.gameObject.SetActive(true),
            ac => ac.gameObject.SetActive(false),
            Destroy
        );

        weights = new float[_audioClips.Length];
        var total = 0f;
        for (var i = 0; i < _audioClips.Length; i++)
        {
            total += 1000f / _audioClips[i].length;
            weights[i] = total;
        }
    }

    private void Update()
    {
        while (_audioQueue.TryPeek(out var curr) && !curr.isPlaying)
        {
            _audioPool.Release(_audioQueue.Dequeue());
        }
    }

    public void PlayAudio(bool unpop = false)
    {
        var instance = _audioPool.Get();

        var r = Random.Range(0f, weights[^1]);
        AudioClip clip = null;
        if (unpop)
        {
            clip = _unpop;
        }
        else
        {
            for (var i = 0; i < _audioClips.Length; i++)
            {
                if (r > weights[i]) continue;
                clip = _audioClips[i];
                break;
            }
        }

        if (clip == null) return;

        instance.clip = clip;
        instance.pitch = Random.Range(0.9f, 1.1f);
        instance.Play();
        _audioQueue.Enqueue(instance);
    }
}