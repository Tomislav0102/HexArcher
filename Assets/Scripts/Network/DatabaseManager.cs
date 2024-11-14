using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;


public class DatabaseManager : MonoBehaviour
{
    [SerializeField] bool useDatabase = true;
    FirebaseFirestore _db;

    string _collectionName = "leaderboard";

    public bool dataLoaded;

    Dictionary<string, int> _dataNamesFromCloud = new Dictionary<string, int>();
    Dictionary<string, int> _dataIdsFromCloud = new Dictionary<string, int>();
    public List<string> names = new List<string>();
    List<string> _ids = new List<string>();
    public List<int> scores = new List<int>();
    public int LocalScore
    {
        get => PlayerPrefs.GetInt(Utils.LbLocalScore_Int);
        set
        {
            int val = value;
            if (val < 0) val = 0;
            PlayerPrefs.SetInt(Utils.LbLocalScore_Int, val);
        }
    }
    
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
        Utils.DatabaseClientSynced += CallEv_DataSynced;
    }

    private void OnDisable()
    {
        Utils.DatabaseClientSynced -= CallEv_DataSynced;
    }

    private void CallEv_DataSynced()
    {
        print("leaderboard data loaded");
        dataLoaded = true;
        
        int myPos = GetMyPositionOnLeaderboard(Utils.MyIdLeaderboard());
        PlayerPrefs.SetInt(Utils.LbRank_Int, myPos);
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
        
        DocumentReference documentReference = _db.Collection(_collectionName).Document(Utils.MyIdLeaderboard());
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
            int myPos = GetMyPositionOnLeaderboard(Utils.MyIdLeaderboard());
            if (myPos < 0 || LocalScore > scores[myPos])
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    {PlayerPrefs.GetString(Utils.PlName_Str), LocalScore.ToString() }
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
        PlayerPrefs.SetInt(Utils.LbRank_Int, -1);
        _dataNamesFromCloud.Clear();
        _dataIdsFromCloud.Clear();

        Query allHighscores = _db.Collection(_collectionName);
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

            Utils.DatabaseClientSynced?.Invoke();
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
        DocumentReference documentReference = _db.Collection(_collectionName).Document(Utils.MyIdLeaderboard());
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
    void M4() => print(GetMyPositionOnLeaderboard(Utils.MyIdLeaderboard()));

}

// public class MyDummyUsernamesClass
// {
//     public string[] usernames;
//     
//     async void GenerateDummyData()
//     {
//         dummyUsernames = JsonUtility.FromJson<MyDummyUsernamesClass>(jsonObj.ToString());
//         for (int i = 0; i < dummyUsernames.usernames.Length; i++)
//         {
//             DocumentReference documentReference = _db.Collection(_collectionName).Document(System.Guid.NewGuid().ToString());
//             Dictionary<string, object> dictionary = new Dictionary<string, object>()
//             {
//                 {dummyUsernames.usernames[i], Random.Range(1, 1000).ToString() }
//             };
//             await documentReference.SetAsync(dictionary);
//         }
//         
//         print("done");
//     }
//
// }
