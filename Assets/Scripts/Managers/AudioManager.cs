using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Unity.Netcode;

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
    int _counter;

    [Title("DJ")]
    [SerializeField] Transform musicSelectionTr;
    [SerializeField] TextMeshProUGUI songNameText;
    [SerializeField] Sprite[] playPauseSprites;
    Image _playImage;
    [SerializeField] Button playButton, prevButton, nextButton;
    enum PlayControls { PlayPause, Previous, Next }
    enum AutoPlayControls { Prev, Next, Random }
    AutoPlayControls _autoPlayControls;
    bool _isPlaying = true;
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
        else
        {
            _playImage = playButton.GetComponent<Image>();
            playButton.onClick.AddListener(() => PlayButtonMethod(PlayControls.PlayPause));
            prevButton.onClick.AddListener(() => PlayButtonMethod(PlayControls.Previous));
            nextButton.onClick.AddListener(() => PlayButtonMethod(PlayControls.Next));
            float yAngle = 90f;
            if (!NetworkManager.Singleton.IsServer) yAngle = -90f;
            musicSelectionTr.localEulerAngles = new Vector3(0, yAngle, 0);
            _autoPlayControls = AutoPlayControls.Random;
            AutomaticPlay();
        }
    }

    void Update()
    {
        if (isMainMenu) return;
        if(!_isPlaying) return;
        
        _timer += Time.deltaTime;
        if (_timer >= audioMusic.clip.length)
        {
            _timer = 0f;
            _autoPlayControls = AutoPlayControls.Random;
            AutomaticPlay();
        }

    }

    void AutomaticPlay()
    {
        switch (_autoPlayControls)
        {
            case AutoPlayControls.Prev:
                _counter--;
                if (_counter < 0) _counter = musicGame.Length - 1;
                break;
            case AutoPlayControls.Next:
                _counter = (1 + _counter) % musicGame.Length;
                break;
            case AutoPlayControls.Random:
                _counter = Random.Range(0, musicGame.Length);
                break;
        }
        audioMusic.clip = musicGame[_counter].clip;
        audioMusic.volume = musicGame[_counter].vol;
        audioMusic.pitch = musicGame[_counter].pitch;
        audioMusic.Play();
        songNameText.text = $"Now playing: {audioMusic.clip.name}";
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

    void PlayButtonMethod(PlayControls playControls)
    {
        switch (playControls)
        {
            case PlayControls.PlayPause:
                _isPlaying = !_isPlaying;
                if (!_isPlaying)
                {
                    audioMusic.Pause();
                    _playImage.sprite = playPauseSprites[0];
                }
                else
                {
                    audioMusic.UnPause();
                    _playImage.sprite = playPauseSprites[1];
                }
                return;
            case PlayControls.Previous:
                audioMusic.UnPause();
                _playImage.sprite = playPauseSprites[0];
                _autoPlayControls = AutoPlayControls.Prev;
                _timer = 0;
                break;
            case PlayControls.Next:
                audioMusic.UnPause();
                _playImage.sprite = playPauseSprites[0];
                _autoPlayControls = AutoPlayControls.Next;
                _timer = 0;
                break;
        }
        
        AutomaticPlay();
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

