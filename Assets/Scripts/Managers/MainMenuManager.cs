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
using UnityEngine.Serialization;
#if(UNITY_EDITOR)
using ParrelSync;
#endif


public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
    
    bool _canUseButtonSounds, _oneHitExitScene, _internetCheckSwitch;
    public bool hasInternet;

    [Title("General")]
    [SerializeField] TextMeshProUGUI displayCoins;
    public GameObject[] mainUiElements;
    public GameObject noInternetWindow, requiredLevelWindow;
    [SerializeField] Button[] buttonsReturn;
    public AudioManager audioManager;
    [Title("Singleplayer - practice")]
    [SerializeField] Button buttonPractice;
    [Title("Singleplayer - campaign")]
    [SerializeField] Button buttonCampaign;
    [Title("Singleplayer - endless")]
    [SerializeField] Button[] buttonsSize;
    [SerializeField] Button[] buttonsDiff;
    [SerializeField] Button[] buttonsStart;
    [Title("Multiplayer")]
    [SerializeField] Button[] buttonSizeMp;
    [SerializeField] Slider windSlider;
    [Title("MP for lobby manager")]
    public TMP_InputField inputPlName;
    public TMP_InputField inputJoinCode;
    public TextMeshProUGUI joinCodeInfo;
    public Button buttonCreate;
    public Button buttonJoin;
    public Button buttonReady;
    [Title("Player level")]
    [SerializeField] TextMeshProUGUI displayLevel;
    [SerializeField] TextMeshProUGUI displayCurrent;
    [SerializeField] TextMeshProUGUI displayToNext;
    
    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Utils.GameType = MainGameType.MainMenu;
        if (Utils.SinglePlayerType == SpType.Practice) PlayerPrefs.SetInt(Utils.TrajectoryVisible_Int, 0);
        Utils.SinglePlayerType = SpType.Endless;
        Utils.CampLevel = 0;

        displayCoins.text = Launch.Instance.myDatabaseManager.GetValAndCastTo<string>(MyData.Coins);
        Utils.ActivateOneArrayElement(mainUiElements, 0);
        BtnMethodMapSize(PlayerPrefs.GetInt(Utils.Size_Int));
        BtnMethodDiff(PlayerPrefs.GetInt(Utils.Difficulty_Int));
        Utils.FadeOut?.Invoke(true);
        Utils.Activation(Launch.Instance.myLobbyManager.gameObject, true);
        if(!Launch.Instance.myDatabaseManager.Clone()) Launch.Instance.myDatabaseManager.DownloadLeaderboard();
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
        buttonPractice.onClick.AddListener(BtnMethodSinglePractice);
        buttonCampaign.onClick.AddListener(BtnMethodSingleCampaign);
        
        buttonsSize[0].onClick.AddListener(() => BtnMethodMapSize(0));
        buttonsSize[1].onClick.AddListener(() => BtnMethodMapSize(1));
        buttonsSize[2].onClick.AddListener(() => BtnMethodMapSize(2));
        buttonSizeMp[0].onClick.AddListener(() => BtnMethodMapSize(0));
        buttonSizeMp[1].onClick.AddListener(() => BtnMethodMapSize(1));
        buttonSizeMp[2].onClick.AddListener(() => BtnMethodMapSize(2));

        buttonsDiff[0].onClick.AddListener(() => BtnMethodDiff(0));
        buttonsDiff[1].onClick.AddListener(() => BtnMethodDiff(1));
        buttonsDiff[2].onClick.AddListener(() => BtnMethodDiff(2));

        for (int i = 0; i < buttonsReturn.Length; i++)
        {
            buttonsReturn[i].onClick.AddListener(BtnMethodReturn);
        }

        for (int i = 0; i < buttonsStart.Length; i++)
        {
            buttonsStart[i].onClick.AddListener(BtnMethodSinglePlay);
        }

        Utils.PlayerXpUpdated += InitPlayerLeveling;
    }


    private void OnDisable()
    {
        buttonPractice.onClick.RemoveAllListeners();
        buttonCampaign.onClick.RemoveAllListeners();

        for (int i = 0; i < 3; i++)
        {
            buttonsSize[i].onClick.RemoveAllListeners();
            buttonSizeMp[i].onClick.RemoveAllListeners();
            buttonsDiff[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < buttonsReturn.Length; i++)
        {
            buttonsReturn[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < buttonsStart.Length; i++)
        {
            buttonsStart[i].onClick.RemoveAllListeners();
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
        Utils.Activation(Launch.Instance.myLobbyManager.gameObject, false);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Unity.Networking.Transport.NetworkEndPoint.AnyIpv4);
        NetworkManager.Singleton.StartHost();
        StartCoroutine(Launch.Instance.mySceneManager.NewSceneAfterFadeIn(MainGameType.Singleplayer));
        Launch.Instance.mySceneManager.SubscribeAll();
        
        PlayerPrefs.SetFloat(Utils.WindAmount_Fl, 0f);
        PlayerPrefs.SetInt(Utils.TrajectoryVisible_Int, Utils.SinglePlayerType == SpType.Practice ? 1 : 0);
    }
    void BtnMethodSinglePractice()
    {
        Utils.SinglePlayerType = SpType.Practice;
        BtnMethodSinglePlay();
    }

    void BtnMethodSingleCampaign()
    {
        Utils.SinglePlayerType = SpType.Campaign;
        BtnMethodSinglePlay();
    }
    void BtnMethodReturn()
    {
        Utils.ActivateOneArrayElement(mainUiElements, 0);
        audioManager.PlaySFX(audioManager.uiButton);
    }
    void BtnMethodMapSize(int ord)
    {
        if (_canUseButtonSounds) audioManager.PlaySFX(audioManager.uiButton);
        PlayerPrefs.SetInt(Utils.Size_Int, ord);
        for (int i = 0; i < 3; i++)
        {
            buttonsSize[i].transform.GetChild(1).gameObject.SetActive(false);
            buttonSizeMp[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        buttonsSize[ord].transform.GetChild(1).gameObject.SetActive(true);
        buttonSizeMp[ord].transform.GetChild(1).gameObject.SetActive(true);
    }
    void BtnMethodDiff(int ord)
    {
        if (_canUseButtonSounds) audioManager.PlaySFX(audioManager.uiButton);
        PlayerPrefs.SetInt(Utils.Difficulty_Int, ord);
        for (int i = 0; i < 3; i++)
        {
            buttonsDiff[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        buttonsDiff[ord].transform.GetChild(1).gameObject.SetActive(true);

    }

    #endregion


    void InitPlayerLeveling()
    {
        PlayerLeveling.CalculateLevelFromXp(out int level, out int xpToNext);
        displayLevel.text = $"Level: {level}";
        displayCurrent.text = $"Current XP: {Launch.Instance.myDatabaseManager.observableData[MyData.Xp]}";
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
