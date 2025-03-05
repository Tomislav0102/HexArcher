using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.Networking;

public class Utils
{
    public static System.Action LeaderboardDataClientSynced;
    public static System.Action GameStarted; //after bot finished first countdown or 2nd player connects
    public static System.Action<bool> FadeOut; //lasts 2 seconds, no need for callback action as a 2nd delegate
    public static System.Action MainUiUnselect;
    public static System.Action PlayerXpUpdated; //probably debug only

    #region STRINGS FOR PLAYERPREFS 
    //suffix is Type
    public static string Difficulty_Int = "difficulty AI";
    public static string Size_Int = "size of grid";
    public static string WindAmount_Fl = "wind amount";
    public static string TrajectoryVisible_Int = "trajectory is visible"; 
    // public static string Bow_Int = "chosen bow player";
    // public static string Head_Int = "head";
    // public static string Hands_Int = "hand";
    // public static string PlName_Str = "name player";
    //public static string PlXp_Int = "experience player";
    // public static string PlLeague_Int = "league player";
    // public static string PlMatches_Int = "total matches player";
    // public static string PlDefeats_Int = "defeats player";
    // public static string PlWins_Int = "wins player";
    static string plLeaderboardId_Str = "id";
     // public static string PlLeaderBoardRank_Int = "leaderboard rank";
     // public static string PlLeaderBoardLocalScore_Int = "local score";
    #endregion

    public static MainGameType GameType;
    public static SpType SinglePlayerType;
    public static int CampLevel;
    public static int WaitTimeStartGame = 2;
    public static Vector2Int ScoreLeaderboardGlobalValues = new Vector2Int(10, -3);


    #region HELPER METHODS
    public static void DisplayAllPlayerPrefs()
    {
        // Debug.Log($"Difficulty: {(GenDifficulty)PlayerPrefs.GetInt(Difficulty_Int)}\n" +
        //     $"Size: {(GenSize)PlayerPrefs.GetInt(Size_Int)}\n" +
        //     $"XP: {PlayerPrefs.GetInt(PlXp_Int)}\n" +
        //     // $"Player name: {PlayerPrefs.GetString(PlName_Str)} \n" +
        //     // "----------------\n" +
        //     // $"Bow: {PlayerPrefs.GetInt(Bow_Int)} \n" +
        //     // $"Head: {PlayerPrefs.GetInt(Head_Int)} \n" +
        //     // $"Hand: {PlayerPrefs.GetInt(Hands_Int)} \n" +
        //     "----------------\n" +
        //     $"Player league: {(League)PlayerPrefs.GetInt(PlLeague_Int)} \n" +
        //     $"Player total matches: {PlayerPrefs.GetInt(PlMatches_Int)} \n" +
        //     $"Player defeats: {PlayerPrefs.GetInt(PlDefeats_Int)} \n" +
        //     $"Player wins: {PlayerPrefs.GetInt(PlWins_Int)} \n" +
        //     "----------------\n" +
        //     $"Wind amount: {PlayerPrefs.GetFloat(WindAmount_Fl)} \n" +
        //     $"Trajectory visible: {PlayerPrefs.GetInt(TrajectoryVisible_Int)} \n" +
        //     "----------------\n" +
        //     $"LB id: {PlayerPrefs.GetString(plLeaderboardId_Str)} \n" +
        //     $"LB rank: {PlayerPrefs.GetInt(PlLeaderBoardRank_Int)} \n" +
        //     $"LB local score: {PlayerPrefs.GetInt(PlLeaderBoardLocalScore_Int)}");
    }

    public static Dictionary<League, Vector3Int> LeaguesTotalDefeatWinsTable = new Dictionary<League, Vector3Int>()
    {
        { League.Bronze1, new Vector3Int(0, 0, 2) },
        { League.Bronze2, new Vector3Int(10, 2, 5) },
        { League.Bronze3, new Vector3Int(10, 3, 6) },
        { League.Silver1, new Vector3Int(15, 5, 9) },
        { League.Silver2, new Vector3Int(15, 6, 10) },
        { League.Silver3, new Vector3Int(15, 7, 10) },
        { League.Gold1, new Vector3Int(20, 9, 13) },
        { League.Gold2, new Vector3Int(20, 9, 14) },
        { League.Gold3, new Vector3Int(20, 10, 14) },
        { League.Platinum1, new Vector3Int(25, 13, 17) },
        { League.Platinum2, new Vector3Int(25, 14, 17) },
        { League.Platinum3, new Vector3Int(25, 14, 18) },
        { League.Diamond1, new Vector3Int(30, 18, 24) },
        { League.Diamond2, new Vector3Int(30, 20, 26) },
        { League.Diamond3, new Vector3Int(30, 25, 30) },
        { League.Challenger, new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue) },
    };

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
    public static void Activation(GameObject go, bool activateGo)
    {
        if (go == null) return;
        if (go.activeInHierarchy != activateGo) go.SetActive(activateGo);
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
           Activation(arr[i], false);
        }
        if (ordinal < arr.Length) Activation(arr[ordinal], true);

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

    public static string MyId()
    {
        if (!PlayerPrefs.HasKey(plLeaderboardId_Str)) PlayerPrefs.SetString(plLeaderboardId_Str, System.Guid.NewGuid().ToString());
        return PlayerPrefs.GetString(plLeaderboardId_Str);
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
    #endregion

}



#region ENUMS
public enum MainGameType { MainMenu, Singleplayer, Multiplayer }
public enum SpType { Endless, Campaign, Practice }
public enum UiDisplays { InfoStart, CampStart, CampEnd, XpEarned }
public enum League : byte
{
    Bronze1, Bronze2, Bronze3, Silver1, Silver2, Silver3, Gold1, Gold2, Gold3, Platinum1, Platinum2, Platinum3, Diamond1, Diamond2, Diamond3, Challenger
}
public enum EncryptionType// Note: Also Udp and Ws are possible choices
{
    DTLS, // Datagram Transport Layer Security
    UDP,
    WSS  // Web Socket Secure
}

public enum GenDifficulty { Easy, Normal, Hard }
public enum GenSize { Small, Medium, Big }
public enum GenSide { Left, Right, Center }
public enum GenResult { Win, Lose, Draw }
public enum GenChange { Increase, Decrease }
public enum TileState { Free, InActive, Taken }
public enum MyData { Id, Name, Xp, League, TotalMatches, Defeats, Wins, BowIndex, HeadIndex, HandsIndex, LeaderboardId, LeaderboardRank, LeaderboardScore }
public enum PlayerColor
{
    Blue,
    Red,
    None,
    Undefined //needed for OnValueChange Callback
}
public enum BowState { RackMoving, RackDone, InHand, Free }
public enum ArrowState { Notched, Flying }
#endregion

#region INTERFACES
public interface ITargetForArrow
{
    void HitMe();
}

public interface ILateInitialization<T>
{
    void InitializeMe(T parentType);
    bool IsInitialized { get; set; }
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


public struct LeagueWrapper : System.IEquatable<LeagueWrapper>, INetworkSerializable
{
    public League value;

    LeagueWrapper(League val)
    {
        this.value = val;
    }

    public bool Equals(LeagueWrapper other)
    {
        return value == other.value;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref value);
    }

    public static implicit operator League(LeagueWrapper wrapper) => wrapper.value;
    public static implicit operator LeagueWrapper(League value) => new LeagueWrapper(value);
}
public struct FixedArrayWrapper : INetworkSerializable
{
    public int[] values;
    public FixedString128Bytes[] names;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            serializer.GetFastBufferWriter().WriteValueSafe(values.Length);
            foreach (int item in values)
            {
                serializer.GetFastBufferWriter().WriteValueSafe(item);
            }
            
            serializer.GetFastBufferWriter().WriteValueSafe(names.Length);
            foreach (FixedString128Bytes item in names)
            {
                serializer.GetFastBufferWriter().WriteValueSafe(item);
            }
        }
        else
        {
            serializer.GetFastBufferReader().ReadValueSafe(out int lenInt);
            values = new int[lenInt];
            for (int i = 0; i < values.Length; i++)
            {
                serializer.GetFastBufferReader().ReadValueSafe(out values[i]);
            }
            
            serializer.GetFastBufferReader().ReadValueSafe(out int lenString128);
            names = new FixedString128Bytes[lenString128];
            for (int i = 0; i < names.Length; i++)
            {
                serializer.GetFastBufferReader().ReadValueSafe(out names[i]);
            }
        }
    }
}
#endregion

