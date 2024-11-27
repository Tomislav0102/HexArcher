using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if(UNITY_EDITOR)
using ParrelSync;
#endif


public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
    
    bool _canUseButtonSounds, _oneHitExitScene, _internetCheckSwitch;
    public bool hasInternet;
    [Title("General")]
    public GameObject[] mainUIelements;
    public GameObject noInternetWindow, requiredLevelWindow;
    [SerializeField] Button[] btnsReturn;
    public AudioManager audioManager;
    [Title("Singleplayer - practice")]
    [SerializeField] Button btnPractice;
    [Title("Singleplayer")]
    [SerializeField] Button[] btnsSize;
    [SerializeField] Button[] btnsDiff;
    [SerializeField] Button[] btnsStart;
    [Title("Multiplayer")]
    [SerializeField] Button[] btnSizeMp;
    [SerializeField] Slider windSlider;
    [Title("MP for lobby manager")]
    public TMP_InputField inputPlName;
    public TMP_InputField inputJoinCode;
    public TextMeshProUGUI joinCodeInfo;
    public Button btnCreate;
    public Button btnJoin;
    public Button btnReady;
    [Title("Player level")]
    [SerializeField] TextMeshProUGUI displayLev;
    [SerializeField] TextMeshProUGUI displayCurrent;
    [SerializeField] TextMeshProUGUI displayToNext;
    
    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Utils.GameType = MainGameType.MainMenu;
        if (Utils.PracticeSp) PlayerPrefs.SetInt(Utils.TrajectoryVisible_Int, 0);
        Utils.PracticeSp = false;
        Utils.ActivateOneArrayElement(mainUIelements, 0);
        BtnMethodMapSize(PlayerPrefs.GetInt(Utils.Size_Int));
        BtnMethodDiff(PlayerPrefs.GetInt(Utils.Difficulty_Int));
        Utils.FadeOut?.Invoke(true);
        Utils.ActivateGo(Launch.Instance.myLobbyManager.gameObject);
        if(!Launch.Instance.myDatabaseManager.AimIclone()) Launch.Instance.myDatabaseManager.DownloadLeaderboard();
        Launch.Instance.myLobbyManager.Init();
        if (NetworkManager.Singleton.IsListening)
        {
            print(("network manager is listening, shutting down network"));
            NetworkManager.Singleton.Shutdown();
        }

        windSlider.value = PlayerPrefs.GetFloat(Utils.WindAmount_Fl);
        windSlider.onValueChanged.AddListener(WindChange);

        _canUseButtonSounds = true;

        InvokeRepeating(nameof(RegularInternetConnectionCheck), 0f, 15f);
        InitPlayerLeveling();
    }

    void RegularInternetConnectionCheck()
    {
        if (_internetCheckSwitch) return;
        _internetCheckSwitch = true;
        StartCoroutine(Utils.CheckInternetConnection((bul) =>
                    {
                        _internetCheckSwitch = false;
                        hasInternet = bul;
                    }));
    }

    private void OnEnable()
    {
        btnPractice.onClick.AddListener(BtnMethodSinglePractice);

        btnsSize[0].onClick.AddListener(() => BtnMethodMapSize(0));
        btnsSize[1].onClick.AddListener(() => BtnMethodMapSize(1));
        btnsSize[2].onClick.AddListener(() => BtnMethodMapSize(2));
        btnSizeMp[0].onClick.AddListener(() => BtnMethodMapSize(0));
        btnSizeMp[1].onClick.AddListener(() => BtnMethodMapSize(1));
        btnSizeMp[2].onClick.AddListener(() => BtnMethodMapSize(2));

        btnsDiff[0].onClick.AddListener(() => BtnMethodDiff(0));
        btnsDiff[1].onClick.AddListener(() => BtnMethodDiff(1));
        btnsDiff[2].onClick.AddListener(() => BtnMethodDiff(2));

        for (int i = 0; i < btnsReturn.Length; i++)
        {
            btnsReturn[i].onClick.AddListener(BtnMethodReturn);
        }

        for (int i = 0; i < btnsStart.Length; i++)
        {
            btnsStart[i].onClick.AddListener(BtnMethodSinglePlay);
        }

        Utils.PlayerXpUpdated += InitPlayerLeveling;
    }


    private void OnDisable()
    {
        btnPractice.onClick.RemoveAllListeners();

        for (int i = 0; i < 3; i++)
        {
            btnsSize[i].onClick.RemoveAllListeners();
            btnSizeMp[i].onClick.RemoveAllListeners();
            btnsDiff[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < btnsReturn.Length; i++)
        {
            btnsReturn[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < btnsStart.Length; i++)
        {
            btnsStart[i].onClick.RemoveAllListeners();
        }
        windSlider.onValueChanged.RemoveAllListeners();
        Utils.PlayerXpUpdated -= InitPlayerLeveling;
        
    }


    void WindChange(float amount) => PlayerPrefs.SetFloat(Utils.WindAmount_Fl, amount);
    
    #region BUTTONS
    [Button]
    void BtnMethodSinglePlay()
    {
        if (_oneHitExitScene) return;
        _oneHitExitScene = true;

        audioManager.PlaySFX(audioManager.uiButton);
        Utils.DeActivateGo(Launch.Instance.myLobbyManager.gameObject);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Unity.Networking.Transport.NetworkEndPoint.AnyIpv4);
        NetworkManager.Singleton.StartHost();
        StartCoroutine(Launch.Instance.mySceneManager.NewSceneAfterFadeIn(MainGameType.Singleplayer));
        Launch.Instance.mySceneManager.SubscribeAll();
        
        if (!Utils.PracticeSp)
        {
            PlayerPrefs.SetFloat(Utils.WindAmount_Fl, 0f);
            PlayerPrefs.SetInt(Utils.TrajectoryVisible_Int, 0);
        }
    }
    void BtnMethodSinglePractice()
    {
        Utils.PracticeSp = true;
        PlayerPrefs.SetInt(Utils.TrajectoryVisible_Int, 1);
        PlayerPrefs.SetFloat(Utils.WindAmount_Fl, 0f);

        BtnMethodSinglePlay();
    }
    void BtnMethodReturn()
    {
        Utils.ActivateOneArrayElement(mainUIelements, 0);
        audioManager.PlaySFX(audioManager.uiButton);
    }
    void BtnMethodMapSize(int ord)
    {
        if (_canUseButtonSounds) audioManager.PlaySFX(audioManager.uiButton);
        PlayerPrefs.SetInt(Utils.Size_Int, ord);
        for (int i = 0; i < 3; i++)
        {
            btnsSize[i].transform.GetChild(1).gameObject.SetActive(false);
            btnSizeMp[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        btnsSize[ord].transform.GetChild(1).gameObject.SetActive(true);
        btnSizeMp[ord].transform.GetChild(1).gameObject.SetActive(true);
    }
    void BtnMethodDiff(int ord)
    {
        if (_canUseButtonSounds) audioManager.PlaySFX(audioManager.uiButton);
        PlayerPrefs.SetInt(Utils.Difficulty_Int, ord);
        for (int i = 0; i < 3; i++)
        {
            btnsDiff[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        btnsDiff[ord].transform.GetChild(1).gameObject.SetActive(true);

    }

    #endregion


    void InitPlayerLeveling()
    {
        PlayerLeveling.CalculateLevelFromXp(out int level, out int xpToNext);
        displayLev.text = $"Level: {level}";
        displayCurrent.text = $"Current XP: {PlayerPrefs.GetInt(Utils.Xp_Int)}";
        displayToNext.text = xpToNext == 0 ? "Max level reached" : $"XP to next level: {xpToNext}";
    }

    #region DEBUGS
    [ContextMenu("Print all playerprefs")]
    void Metoda3()
    {
        Utils.DisplayAllPlayerPrefs();
    }

    public void BtnLevelUp()
    {
        PlayerLeveling.GetMeToLevel(5);
        Utils.PlayerXpUpdated?.Invoke();
    }
    #endregion


}
