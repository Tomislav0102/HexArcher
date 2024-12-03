using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.Networking;

public class Utils
{
    public static System.Action DatabaseClientSynced;
    public static System.Action GameStarted; //after bot finished first countdown or 2nd player connects
    public static System.Action<bool> FadeOut; //lasts 2 seconds, no need for callback action as a 2nd delegate
    public static System.Action MainUiUnselect;
    public static System.Action PlayerXpUpdated;

    #region STRINGS FOR PLAYERPREFS 
    //suffix is Type
    public static string Difficulty_Int = "difficulty AI";
    public static string Size_Int = "size of grid";
    public static string Xp_Int = "experience player";
    public static string Bow_Int = "chosen bow player";
    public static string PlName_Str = "name player";
    public static string WindAmount_Fl = "wind amount";
    public static string TrajectoryVisible_Int = "trajectory is visible"; 
    static string lBidStr = "leaderboard id";
    public static string LbRank_Int = "leaderboard rank";
    public static string LbLocalScore_Int = "local score";
    #endregion

    public static MainGameType GameType;
    public static bool PracticeSp;
    public static int WaitTimeStartGame = 2;
    public static Vector2Int ScoreGlobalValues = new Vector2Int(10, -3);



    public static void DisplayAllPlayerPrefs()
    {
        Debug.Log($"Difficulty: {(GenLevel)PlayerPrefs.GetInt(Difficulty_Int)}\n" +
            $"Size: {(GenSize)PlayerPrefs.GetInt(Size_Int)}\n" +
            $"XP: {PlayerPrefs.GetInt(Xp_Int)}\n" +
            $"Bow: {PlayerPrefs.GetInt(Bow_Int)} \n" +
            $"Player name: {PlayerPrefs.GetString(PlName_Str)} \n" +
            $"Wind amount: {PlayerPrefs.GetFloat(WindAmount_Fl)} \n" +
            $"Trajectory visible: {PlayerPrefs.GetInt(TrajectoryVisible_Int)} \n" +
            $"LB id: {PlayerPrefs.GetString(lBidStr)} \n" +
            $"LB rank: {PlayerPrefs.GetInt(LbRank_Int)} \n" +
            $"LB local score: {PlayerPrefs.GetInt(LbLocalScore_Int)}");
    }


    public static IEnumerator CheckInternetConnection(System.Action<bool> isConnected)
    {
        UnityWebRequest request = new UnityWebRequest("https://google.com");
        yield return request.SendWebRequest();
        if (request.error == null)
        {
           // Debug.Log("success, connected to internet");
            isConnected?.Invoke(true);
        }
        else
        {
           // Debug.Log("no internet connection");
            isConnected?.Invoke(false);
        }
    }
    public static void ActivateGo(GameObject go)
    {   
        if (go != null && !go.activeInHierarchy) go.SetActive(true);
    }
    public static void DeActivateGo(GameObject go)
    {
        if (go != null && go.activeInHierarchy) go.SetActive(false);
    }

    public static void DestroyGo(GameObject go)
    {
        if (go != null) GameObject.Destroy(go);
    }

    public static GameObject[] AllChildrenGameObjects(Transform parGos)
    {
        GameObject[] gos = new GameObject[parGos.childCount];
        for (int i = 0; i < gos.Length; ++i)
        {
            gos[i] = parGos.GetChild(i).gameObject;
        }
        return gos;
    }
    public static T[] AllChildren<T>(Transform parTransform) where T : Component
    {
        T[] children = new T[parTransform.childCount];
        for (int i = 0; i < children.Length; ++i)
        {
            children[i] = parTransform.GetChild(i).GetComponent<T>();
        }
        return children;
    }
    public static void ActivateOneArrayElement(GameObject[] arr, int ordinal = System.Int32.MaxValue)
    {
        for (int i = 0; i < arr.Length; i++)
        {
           DeActivateGo(arr[i]);
        }
        if (ordinal < arr.Length) ActivateGo(arr[ordinal]);

    }
    static readonly Dictionary<float, WaitForSeconds> WaitDictionary = new Dictionary<float, WaitForSeconds>();
    public static WaitForSeconds GetWait(float time)
    {
        if (WaitDictionary.TryGetValue(time, out WaitForSeconds wait)) return wait;
        WaitDictionary[time] = new WaitForSeconds(time);
        return WaitDictionary[time];
    }
    public static List<int> RandomList(int size)
    {
        List<int> nums = Enumerable.Range(0, size).ToList();
        var rnd = new System.Random();
        var randNums = nums.OrderBy(n => rnd.Next());
        List<int> list = new List<int>();
        foreach (var item in randNums)
        {
            list.Add(item);
        }
        return list;
    }
    public static List<T> RandomListByType<T>(List<T> listToRandomize)
    {
        var rnd = new System.Random();
        var randNums = listToRandomize.OrderBy(n => rnd.Next());
        List<T> list = new List<T>();
        foreach (var item in randNums)
        {
            list.Add(item);
        }
        return list;
    }

    public static string MyIdLeaderboard()
    {
        if (!PlayerPrefs.HasKey(lBidStr)) PlayerPrefs.SetString(lBidStr, System.Guid.NewGuid().ToString());
        return PlayerPrefs.GetString(lBidStr);
    }

    public static string[] PurgedString(string stInput)
    {
        stInput = stInput.Replace("\r\n\r\n", ".");
        stInput = stInput.Replace("\r\n", ".");
        return stInput.Split('.');
    }
    public static string AdjustedGuid(int length = -1)
    {
        string st = System.Guid.NewGuid().ToString();
        st = st.Replace("-", "");
        if (length > 0) st = st.Substring(0, length);

        return st;
    }

}



#region ENUMS
public enum MainGameType
{
    MainMenu,
    Singleplayer,
    Multiplayer
}
public enum EncryptionType
{
    DTLS, // Datagram Transport Layer Security
    UDP,
    WSS  // Web Socket Secure
}
// Note: Also Udp and Ws are possible choices
public enum GenLevel
{
    Easy,
    Normal,
    Hard
}
public enum GenSize
{
    Small,
    Medium,
    Big
}
public enum GenSide
{
    Left,
    Right,
    Center
}
public enum GenFinish
{
    Win,
    Lose,
    Draw
}
public enum TileState
{
    Free,
    InActive,
    Taken
}
public enum PlayerColor
{
    Blue,
    Red,
    None,
    Undefined //needed for OnValueChange Callback
}
public enum BowState
{
    RackMoving,
    RackDone,
    InHand,
    Free
}
public enum ArrowState
{
    Notched,
    Flying
}
#endregion

#region INTERFACES
public interface ITargetForArrow
{
    void HitMe();
}
#endregion

#region NETWORK SERIALIZATION
public struct NetworkString : INetworkSerializable
{
    FixedString32Bytes _myString;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _myString);
    }
    public override string ToString()
    {
        return _myString.ToString();
    }
    public static implicit operator string(NetworkString str) => str.ToString();
    public static implicit operator NetworkString(string str) => new NetworkString()
    {
        _myString = new FixedString32Bytes(str)
    };
}

public struct NetTransform : INetworkSerializable
{
    public Vector3 pos;
    public Quaternion rot;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref pos);
        serializer.SerializeValue(ref rot);

    }
}
public struct NetHexState : INetworkSerializable
{
    public Vector2Int pos;
    public sbyte val;
    public TileState tState;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref pos);
        serializer.SerializeValue(ref val);
        serializer.SerializeValue(ref tState);
    }
}

//not being used
public struct PlayerDataNet : System.IEquatable<PlayerDataNet>, INetworkSerializable
{
    public ulong clientId;
    public int colorId;
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;


    public bool Equals(PlayerDataNet other)
    {
        return
            clientId == other.clientId &&
            colorId == other.colorId &&
            playerName == other.playerName &&
            playerId == other.playerId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref colorId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
    }

}
#endregion



//#region DATABASE
//[System.Serializable]
//public class SingleEntryClass
//{
//    public string id;
//    public string nick;
//    public int score;
//}
//[FirestoreData]
//public class SingleEntryStruct
//{
//    [FirestoreProperty] public string Id { get; set; }
//    [FirestoreProperty] public string Nick { get; set; }
//    [FirestoreProperty] public int Score { get; set; }
//}
//[System.Serializable]
//public class LeaderClass
//{
//    public List<SingleEntryClass> entries = new List<SingleEntryClass>();

//    SingleEntryClass GetClassFromStructSingle(SingleEntryStruct seStruct)
//    {
//        SingleEntryClass nickScoreClass = new SingleEntryClass()
//        {
//            id = seStruct.Id,
//            nick = seStruct.Nick,
//            score = seStruct.Score
//        };

//        return nickScoreClass;
//    }
//    SingleEntryStruct GetStructFromClassSingle(SingleEntryClass seClass)
//    {
//        SingleEntryStruct nickScoreStruct = new SingleEntryStruct()
//        {
//            Id = seClass.id,
//            Nick = seClass.nick,
//            Score = seClass.score
//        };

//        return nickScoreStruct;
//    }
//    LeaderClass GetClassFromStructLeader(LeaderStruct ls)
//    {
//        List<SingleEntryClass> list = new List<SingleEntryClass>();
//        for (int i = 0; i < ls.Entries.Count; i++)
//        {
//            list.Add(GetClassFromStructSingle(ls.Entries[i]));
//        }

//        return new LeaderClass()
//        {
//            entries = list
//        };
//    }
//    LeaderStruct GetStructFromClassLeader(LeaderClass lc)
//    {
//        List<SingleEntryStruct> list = new List<SingleEntryStruct>();
//        for (int i = 0; i < lc.entries.Count; i++)
//        {
//            list.Add(GetStructFromClassSingle(lc.entries[i]));
//        }

//        return new LeaderStruct()
//        {
//            Entries = list
//        };
//    }

//}
//[FirestoreData]
//public struct LeaderStruct
//{
//    [FirestoreProperty] public List<SingleEntryStruct> Entries { get; set; }
//}

//#endregion


//
// public class ParentHex : TileParent
// {
//     GameManager gm;
//     [Title("Class specific")]
//     public Transform center;
//     [SerializeField] AudioSource aSource;
//     [SerializeField] Transform parColliders, parDisplays, parSpritesOrange, parSpritesTeal;
//     Collider2D[] _colliders;
//     GameObject[] _displayValues;
//     GameObject[] _spritesOrange, _spritesTeal;
//     const int CONST_HexNum = 10;
//
//     [Title("Final Hex")]
//     [SerializeField] Transform finalHexagonTransform;
//     MeshRenderer _finalHexMesh;
//     [SerializeField] TextMeshPro finalHexScore;
//     [SerializeField] MeshRenderer finalHexFrameMesh;
//
//     public TileState Tstate
//     {
//         get => _tState;
//         set
//         {
//             _tState = value;
//             gm.hexStateNet[transform.GetSiblingIndex()] = (byte)value;
//             switch (value)
//             {
//                 case TileState.Free: //can be hit
//                     break;
//                 case TileState.InActive: //disabled, not part of game
//                     Utils.DeActivateGo(gameObject);
//                     break;
//                 case TileState.Taken: //can't be hit anymore
//                     _allAnimationsDone?.Invoke();
//                     for (int i = 0; i < CONST_HexNum; i++)
//                     {
//                         _colliders[i].enabled = false;
//                     }
//                     UpdateTakenHex_EveryoneRpc(CurrentValue);
//                     gm.Scoring_ServerRpc();
//                     gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
//                     break;
//             }
//
//         }
//     }
//     public TileState startTs;
//     
//     [ShowInInspector] 
//     [ReadOnly] 
//     List<ParentHex> _myNeighbours;
//
//     /// <summary>
//     /// Most important variable for this monobehaviour.
//     /// Positive value means points for blue, negative is points for red.
//     /// </summary>
//     public sbyte CurrentValue
//     {
//         get => _currentValueNet.Value;
//         private set
//         {
//            _currentValueNet.Value = value;
//             sbyte positiveValue = (sbyte)Mathf.Abs(value);
//             for (int i = 0; i < CONST_HexNum; i++)
//             {
//                 _colliders[i].enabled = i >= positiveValue;
//             }
//
//             UpdateColors_EveryoneRpc(value);
//
//             if (positiveValue > 9)
//             {
//                 _currentValueNet.Value = _hitValue;
//                 Tstate = TileState.Taken;
//             }
//             else UpdateTexts_EveryoneRpc(positiveValue);
//
//         }
//     }
//     NetworkVariable<sbyte> _currentValueNet = new NetworkVariable<sbyte>();
//
//
//     sbyte PlayerModifier() => (sbyte)(gm.playerTurnNet.Value == PlayerColor.Blue ? 1 : -1);
//     const float CONST_Time = 0.2f;
//
//     /// <summary>
//     /// Turn is over after all this hex and all its neighbours have updated their 'CurrentValue'.
//     /// </summary>
//     int NeighboursCheckIn
//     {
//         get => _neighboursCheckIn;
//         set
//         {
//             _neighboursCheckIn = value;
//             gm.audioManager.PlaySFX(gm.audioManager.hexCounter, finalHexagonTransform);
//             if (value <= 0) //all neighbours have finished their animations
//             {
//                 if (_oneShotNeighbourCheckIn) return;
//                 _oneShotNeighbourCheckIn = true;
//                 gm.NextPlayer_ServerRpc(false, "NeighboursCheckIn");
//             }
//         }
//     }
//     [ShowInInspector]
//     [ReadOnly]
//     int _neighboursCheckIn; 
//     bool _oneShotNeighbourCheckIn; //safety bool, makes sure '_gm.NextPlayer()' is triggered only once, probably redundant
//     System.Action _allAnimationsDone; //event used to communicate between hex and its neighbours
//     sbyte _hitValue;
//
//     [HideInInspector] public int valueForBot; 
//     [Title("Testing")]
//     public sbyte hitValueTest = 8;
//     [Button]
//     void TestHit() => HexHit(hitValueTest, gm.playerTurnNet.Value);
//
//
//     private void Awake()
//     {
//         gm = GameManager.Instance;
//         _colliders = Utils.AllChildren<Collider2D>(parColliders);
//         _displayValues = Utils.AllChildrenGameObjects(parDisplays);
//         _spritesOrange = Utils.AllChildrenGameObjects(parSpritesOrange);
//         _spritesTeal = Utils.AllChildrenGameObjects(parSpritesTeal);
//         _finalHexMesh = finalHexagonTransform.GetComponent<MeshRenderer>();
//     }
//
//     void Start()
//     {
//         Tstate = startTs;
//         CurrentValue = 0;
//     }
//
//
//     public void HexHit(sbyte ordinal, PlayerColor arrowColor)
//     {
//         if (Tstate == TileState.InActive || Tstate == TileState.Taken) return;
//
//         gm.audioManager.PlayOnMyAudioSource(aSource, gm.audioManager.hexHit);
//         int mod = arrowColor == PlayerColor.Blue ? 1 : -1;
//         CurrentValue = _hitValue = (sbyte)(ordinal * mod);
//         Tstate = TileState.Taken;
//         Utils.ActivateOneArrayElement(_displayValues);
//
//         _myNeighbours = gm.gridManager.AllNeighbours(pos);
//         NeighboursCheckIn = _myNeighbours.Count;
//         if (_myNeighbours.Count == 0)
//         {
//             gm.NextPlayer_ServerRpc(false, "HexHit");
//             return;
//         }
//         foreach (ParentHex item in _myNeighbours)
//         {
//             item.ActivateFromDirectHit(CurrentValue, () => NeighboursCheckIn--);
//         }
//     }
//
//     public void ClientLateJoin(byte stateFromByte, sbyte val)
//     {
//         sbyte positiveValue = (sbyte)Mathf.Abs(val);
//         switch ((TileState)stateFromByte)
//         {
//             case TileState.Free:
//                 switch (val)
//                 {
//                     case > 0:
//                         spriteRenderer.enabled = false;
//                         Utils.ActivateOneArrayElement(_displayValues, positiveValue);
//                         _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
//                         Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
//                         break;
//                     case < 0:
//                         spriteRenderer.enabled = false;
//                         Utils.ActivateOneArrayElement(_displayValues, positiveValue);
//                         _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
//                         Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
//                         break;
//                 }
//                 break;
//             case TileState.InActive:
//                 Utils.DeActivateGo(gameObject);
//                 return;
//             case TileState.Taken:
//                 if (positiveValue != 0)
//                 {
//                     spriteRenderer.enabled = false;
//                     Utils.ActivateOneArrayElement(_spritesTeal);
//                     Utils.ActivateOneArrayElement(_spritesOrange);
//                     Utils.ActivateOneArrayElement(_displayValues);
//                     finalHexScore.text = positiveValue.ToString();
//                     finalHexScore.color = gm.playerDatas[val > 0 ? 0 : 1].colMain;
//                     finalHexScore.enabled = true;
//                     finalHexFrameMesh.material = gm.playerDatas[val > 0 ? 0 : 1].matMain;
//                     finalHexFrameMesh.enabled = true;
//                     finalHexagonTransform.localEulerAngles = new Vector3(60f, 90f, -90f);
//                 }
//                 if (val > 0) _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
//                 else _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
//                 break;
//         }
//
//         enabled = false;
//     }
//
//     #region RPC CALLS
//     [Rpc(SendTo.Server)]
//     void SetCurrentValue_ServerRpc(sbyte bat) =>  _currentValueNet.Value = bat;
//     [Rpc(SendTo.Everyone)]
//     void UpdateColors_EveryoneRpc(sbyte value)
//     {
//         sbyte positiveValue = (sbyte)Mathf.Abs(value);
//         spriteRenderer.enabled = false;
//         Utils.ActivateOneArrayElement(_spritesTeal);
//         Utils.ActivateOneArrayElement(_spritesOrange);
//         switch (value)
//         {
//             case > 0:
//                 _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
//                 Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
//                 break;
//             case < 0:
//                 _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
//                 Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
//                 break;
//             default:
//                 _finalHexMesh.material = gm.playerDatas[2].matsHex[1];
//                 spriteRenderer.enabled = true;
//                 break;
//         }
//     }
//     [Rpc(SendTo.Everyone)]
//     void UpdateTexts_EveryoneRpc(sbyte val, bool hideAll = false)
//     {
//         if(hideAll) Utils.ActivateOneArrayElement(_displayValues);
//         else Utils.ActivateOneArrayElement(_displayValues, val);
//     }
//
//     [Rpc(SendTo.Everyone)]
//     void HexAnimationFromPool_EveryoneRpc(Vector3 spawnPosition, sbyte val)
//     {
//         RingsPooled hr = gm.poolManager.GetHexRing();
//         hr.SpawnMeOnClient(spawnPosition, val);
//     }
//     [Rpc(SendTo.Everyone)]
//     void UpdateTakenHex_EveryoneRpc(sbyte val) 
//     {
//         spriteRenderer.enabled = false;
//
//         Utils.ActivateOneArrayElement(_spritesTeal);
//         Utils.ActivateOneArrayElement(_spritesOrange);
//         Utils.ActivateOneArrayElement(_displayValues);
//         finalHexScore.text = Mathf.Abs(val).ToString();
//         finalHexScore.color = gm.playerDatas[val > 0 ? 0 : 1].colMain;
//         finalHexScore.enabled = true;
//         finalHexFrameMesh.material = gm.playerDatas[val > 0 ? 0 : 1].matMain;
//         finalHexFrameMesh.enabled = true;
//
//         finalHexagonTransform.DOLocalRotate(new Vector3(60f, 90f, -90f), CONST_Time);
//     }
//     #endregion
//
//
//     #region NEIGHBOUR BEHAVIOUR 
//     /// <summary>
//     /// This method is called if one of its neighbour is hit directly.
//     /// </summary>
//     /// <param name="hitValue">Hit value of hit neighbour.</param>
//     /// <param name="callbackFinishedAnimation">Reports to 'ParentHex' that started it, that animation is done and 'CurrentValue' is updated.</param>
//     void ActivateFromDirectHit(sbyte hitValue, System.Action callbackFinishedAnimation)
//     {
//         _hitValue = (sbyte)(hitValue + CurrentValue);
//         _allAnimationsDone = callbackFinishedAnimation;
//         CheckCurrentValue();
//     }
//     
//
//     /// <summary>
//     /// This method and 'AnimateHexagon' call each other until 'CurrentValue' is updated.
//     /// This should be done in one frame (and one method) but is separated because of animations
//     /// </summary>
//     void CheckCurrentValue()
//     {
//         if (Tstate == TileState.Taken) return;
//
//         if (_hitValue > CurrentValue) //blue
//         {
//             if (_hitValue != CurrentValue) AnimateHexagon(CurrentValue >= 0, 1);
//         }
//         else if (_hitValue < CurrentValue) //red
//         {
//             if (_hitValue != CurrentValue) AnimateHexagon(CurrentValue <= 0, -1);
//         }
//         else
//         {
//             _allAnimationsDone?.Invoke();
//             _allAnimationsDone = null;
//             gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
//         }
//
//     }
//     void AnimateHexagon(bool increase, sbyte currValueChange)
//     {
//         if (Tstate == TileState.Taken) return;
//
//         RingsPooled hr = gm.poolManager.GetHexRing();
//         hr.SpawnMe(center.position, CurrentValue, () =>
//         {
//             CurrentValue += currValueChange;
//             CheckCurrentValue();
//         });
//         HexAnimationFromPool_EveryoneRpc(center.position, CurrentValue);
//         if (CurrentValue == 0) return;
//         if (increase) UpdateColors_EveryoneRpc((sbyte)(CurrentValue + currValueChange));
//         else UpdateColors_EveryoneRpc((sbyte)(CurrentValue - currValueChange));
//     }
//
//     #endregion
// }