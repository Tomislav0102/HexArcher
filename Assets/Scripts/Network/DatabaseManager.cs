using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
#if(UNITY_EDITOR)
using ParrelSync;
#endif


public class DatabaseManager : SerializedMonoBehaviour
{
    [SerializeField] bool useDatabase = true;
    FirebaseFirestore _db;

    string _collectionNameLeaderboard = "leaderboard";

    public bool dataLeaderboardLoaded;

    Dictionary<string, int> _dataNamesFromCloud = new Dictionary<string, int>();
    Dictionary<string, int> _dataIdsFromCloud = new Dictionary<string, int>();
    [HideInInspector] public List<string> names = new List<string>();
    List<string> _ids = new List<string>();
    [HideInInspector] public List<int> scores = new List<int>();
    public int LocalScore
    {
        get => GetValFromKeyEnum<int>(MyData.LeaderboardScore); 
        set
        {
            int val = value;
            if (val < 0) val = 0;
            _myData[MyData.LeaderboardScore] = val.ToString();
        }
    }

    [Title("My data")]
    public Dictionary<MyData, string> EncapsulatedData
    {
        get
        {
#if !UNITY_EDITOR
        myData[MyData.Name] = PlayerPrefs.GetString(Utils.PlName_Str);
        myData[MyData.Xp] = PlayerPrefs.GetInt(Utils.PlXp_Int).ToString();
        myData[MyData.League] = PlayerPrefs.GetInt(Utils.PlLeague_Int).ToString();
        myData[MyData.TotalMatches] = PlayerPrefs.GetInt(Utils.PlMatches_Int).ToString();
        myData[MyData.Defeats] = PlayerPrefs.GetInt(Utils.PlDefeats_Int).ToString();
        myData[MyData.Wins] = PlayerPrefs.GetInt(Utils.PlWins_Int).ToString();
        myData[MyData.BowIndex] = PlayerPrefs.GetInt(Utils.Bow_Int).ToString();
        myData[MyData.HeadIndex] = PlayerPrefs.GetInt(Utils.Head_Int).ToString();
        myData[MyData.HandsIndex] = PlayerPrefs.GetInt(Utils.Hands_Int).ToString();
        myData[MyData.LeaderboardId] = Utils.MyId();
        myData[MyData.LeaderboardRank] = PlayerPrefs.GetInt(Utils.PlLeaderBoardRank_Int).ToString();
        myData[MyData.LeaderboardScore] = PlayerPrefs.GetInt(Utils.PlLeaderBoardLocalScore_Int).ToString();
#endif

            return _myData;
        }
        set
        {
            _myData = value;
#if !UNITY_EDITOR
            foreach (KeyValuePair<MyData,string> item in value)
            {
                switch (item.Key)
                {
                    case MyData.Id:
                        break;
                    case MyData.Name:
                        PlayerPrefs.SetString(Utils.PlName_Str, item.Value);
                        break;
                    case MyData.Xp:
                        PlayerPrefs.SetInt(Utils.PlXp_Int, int.Parse(item.Value));
                        break;
                    case MyData.League:
                        PlayerPrefs.SetInt(Utils.PlLeague_Int, int.Parse(item.Value));
                        break;
                    case MyData.TotalMatches:
                        PlayerPrefs.SetInt(Utils.PlMatches_Int, int.Parse(item.Value));
                        break;
                    case MyData.Defeats:
                        PlayerPrefs.SetInt(Utils.PlDefeats_Int, int.Parse(item.Value));
                        break;
                    case MyData.Wins:
                        PlayerPrefs.SetInt(Utils.PlWins_Int, int.Parse(item.Value));
                        break;
                    case MyData.BowIndex:
                        PlayerPrefs.SetInt(Utils.Bow_Int, int.Parse(item.Value));
                        break;
                    case MyData.HeadIndex:
                        PlayerPrefs.SetInt(Utils.Head_Int, int.Parse(item.Value));
                        break;
                    case MyData.HandsIndex:
                        PlayerPrefs.SetInt(Utils.Hands_Int, int.Parse(item.Value));
                        break;
                    case MyData.LeaderboardRank:
                        PlayerPrefs.SetInt(Utils.PlLeaderBoardRank_Int, int.Parse(item.Value));
                        break;
                    case MyData.LeaderboardScore:
                        PlayerPrefs.SetInt(Utils.PlLeaderBoardLocalScore_Int, int.Parse(item.Value));
                        break;
                }
            }
#endif
        }
    } 
    public Dictionary<MyData, string> _myData = new Dictionary<MyData, string>(); 
    
    
    
    private void Awake()
    {
        _db = FirebaseFirestore.DefaultInstance;
    }
    private void Start()
    {
        DontDestroyOnLoad(this);
    }
    private void OnEnable()
    {
        if(!Clone()) Utils.LeaderboardDataClientSynced += CallEv_LeaderboardDataSynced;
    }

    private void OnDisable()
    {
        if(!Clone()) Utils.LeaderboardDataClientSynced -= CallEv_LeaderboardDataSynced;
    }

    public bool Clone()
    {
        #if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            return true;
        }
        #endif
        return false;
    }

    #region LEADERBOARD
    private void CallEv_LeaderboardDataSynced()
    {
        print("leaderboard data loaded");
        dataLeaderboardLoaded = true;
        
        int myPos = GetMyPositionOnLeaderboard(Utils.MyId());
        _myData[MyData.LeaderboardRank] = myPos.ToString();
        if (myPos < 0) return; //no entry in LB
        if (LocalScore > scores[myPos]) LocalScore = scores[myPos]; //user has tampered with playerprefs. local score can't be higher than cloud score
    }
    
    [ContextMenu("My upload")]
    public void UploadMyScore()
    {
        if (!useDatabase) return;
        if (LocalScore < 1)
        {
            print("My score is less than 1, no upload");
            return;
        }
        
        DocumentReference documentReference = _db.Collection(_collectionNameLeaderboard).Document(Utils.MyId());
        documentReference.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot snapShot = task.Result;
            if (snapShot.Exists)
            {
                print("snapshot exists");
                Dictionary<string, object> data = snapShot.ToDictionary();
                foreach (KeyValuePair<string,object> item in data)
                {
                    int scoreOnLeaderboard = int.Parse(item.Value.ToString());
                    if (LocalScore > scoreOnLeaderboard)
                    {
                        UploadFinal();
                    }
                    else
                    {
                        print("current score isn't greater that high score, no upload");
                    }
                }
            }
            else
            {
                print("no snapshot found");
                UploadFinal();
            }
        });

        void UploadFinal()
        {
            int myPos = GetMyPositionOnLeaderboard(Utils.MyId());
            if (myPos < 0 || LocalScore > scores[myPos])
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    {_myData[MyData.Name], LocalScore.ToString() }
                };

                documentReference.SetAsync(dictionary).ContinueWithOnMainThread(task1 =>
                {
                    print("uploaded my score to leaderboard");
                    DownloadLeaderboard();
                });
            }
            else
            {
                print("local score is lower than leaderboard score, no upload");
            }
        }
    }
    
    public void DownloadLeaderboard()
    {
        if (!useDatabase) return;
        _myData[MyData.LeaderboardRank] = "-1";
        _dataNamesFromCloud.Clear();
        _dataIdsFromCloud.Clear();

        Query allHighscores = _db.Collection(_collectionNameLeaderboard);
        allHighscores.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            QuerySnapshot allHighscoreesSnapshot = task.Result;
            foreach (DocumentSnapshot item in allHighscoreesSnapshot)
            {
                Dictionary<string, object> dic = item.ToDictionary();
                foreach (KeyValuePair<string, object> dictionaryItem in dic)
                {
                    _dataNamesFromCloud.Add(dictionaryItem.Key, System.Convert.ToInt32(dictionaryItem.Value));
                    _dataIdsFromCloud.Add(item.Id, System.Convert.ToInt32(dictionaryItem.Value));
                }
            }
            SortDictionaryIntoLists();

            Utils.LeaderboardDataClientSynced?.Invoke();
        });


        void SortDictionaryIntoLists()
        {
            names.Clear();
            scores.Clear();
            _ids.Clear();
            foreach (KeyValuePair<string, int> item in _dataNamesFromCloud)
            {
                scores.Add(item.Value);
            }
            scores.Sort();
            scores.Reverse();

            for (int i = 0; i < scores.Count; i++)
            {
                foreach (KeyValuePair<string, int> item in _dataNamesFromCloud)
                {
                    if (item.Value == scores[i])
                    {
                        names.Add(item.Key);
                        _dataNamesFromCloud.Remove(item.Key);
                        break;
                    }
                }
                foreach (KeyValuePair<string, int> item in _dataIdsFromCloud)
                {
                    if (item.Value == scores[i])
                    {
                        _ids.Add(item.Key);
                        _dataIdsFromCloud.Remove(item.Key);
                        break;
                    }
                }
            }
        }
    }
    [ContextMenu("Remove my entry")]
    void RemoveMyEntry()
    {
        DocumentReference documentReference = _db.Collection(_collectionNameLeaderboard).Document(Utils.MyId());
        documentReference.DeleteAsync().ContinueWithOnMainThread(task =>
        {
            print("entry deleted");
            LocalScore = 0;
            DownloadLeaderboard();
        });
    }
    public int GetMyPositionOnLeaderboard(string id)
    {
        for (int i = 0; i < _ids.Count; i++)
        {
            if (_ids[i] == id) return i;
        }
        return -1;
    }   
    
    [ContextMenu("Print all playerprefs")]
    void M3() => Utils.DisplayAllPlayerPrefs();

    [ContextMenu("My pos on LB")]
    void M4() => print(GetMyPositionOnLeaderboard(Utils.MyId()));

    #endregion

    public T GetValFromKeyEnum<T>(MyData dataEnum) where T : notnull => (T)System.Convert.ChangeType(_myData[dataEnum], typeof(T));


}

