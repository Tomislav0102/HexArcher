using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSFX, audioMusic;

    [Title("Hex")]
    public AudioData hexHit;
    public AudioData hexCounter;
    [Title("Bow")]
    public AudioData bowDraw;
    public AudioData bowRelease;
    public AudioData arrowHit;
    [Title("Game flow")]
    public AudioData hoverEnter;
    public AudioData uiButton;
    public AudioData gameStarted;
    public AudioData win;
    public AudioData loose;
    public AudioData draw;

    [Title(("Music"))]
    [SerializeField] bool isMainMenu;
    [SerializeField] AudioData musicMainMenu;
    [SerializeField] AudioData[] musicGame;
    float _timer;
    void Start()
    {
        if (isMainMenu)
        {
            audioMusic.clip = musicMainMenu.clip;
            audioMusic.volume = musicMainMenu.vol;
            audioMusic.pitch = musicMainMenu.pitch;
            audioMusic.loop = true;
            audioMusic.Play();
        }
    }

    void Update()
    {
        if (isMainMenu) return;

        if (_timer == 0f)
        {
            int counter = Random.Range(0, musicGame.Length);
            audioMusic.clip = musicGame[counter].clip;
            audioMusic.volume = musicGame[counter].vol;
            audioMusic.pitch = musicGame[counter].pitch;
            audioMusic.Play();
        }
        _timer += Time.deltaTime;
        if(_timer >= audioMusic.clip.length) _timer = 0f;
    }


    public void PlaySFX(AudioData aData, Transform trPosition = null)
    {
        if (aData == null || aData.clip == null) return;
        float vol = aData.vol;
        audioSFX.volume = vol;
        audioSFX.pitch = aData.pitch;
        if(trPosition == null)
        {
            audioSFX.clip = aData.clip;
            audioSFX.Play();
        }
        else
        {
            AudioSource.PlayClipAtPoint(aData.clip, trPosition.position, vol);
        }
    }

    public void PlayOnMyAudioSource(AudioSource source, AudioData aData)
    {
        if (aData == null || aData.clip == null) return;
        if (source.isPlaying) source.Stop();

        source.volume = aData.vol;
        source.pitch = aData.pitch;
        source.clip = aData.clip;
        source.Play();
    }




}

[System.Serializable]
public class AudioData
{
    [HideLabel, HorizontalGroup(Width = 0.5f)]
    public AudioClip clip;
    [HorizontalGroup(Width = 0f, PaddingLeft = 0.05f), LabelWidth(20)]
    public float vol = 1f;
    [HorizontalGroup(Width = 0f, PaddingLeft = 0.05f), LabelWidth(30)]
    public float pitch = 1f;
}

