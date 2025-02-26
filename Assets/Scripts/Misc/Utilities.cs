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
    public static string WindAmount_Fl = "wind amount";
    public static string TrajectoryVisible_Int = "trajectory is visible"; 
    public static string Bow_Int = "chosen bow player";
    public static string PlName_Str = "name player";
    public static string PlXp_Int = "experience player";
    public static string PlRank_Int = "rank player";
    static string plLeaderBoardIdStr = "leaderboard id";
    public static string PlLeaderBoardRank_Int = "leaderboard rank";
    public static string PlLeaderBoardLocalScore_Int = "local score";
    #endregion

    public static MainGameType GameType;
    public static SpType SinglePlayerType;
    public static int CampLevel;
    public static int WaitTimeStartGame = 2;
    public static Vector2Int ScoreGlobalValues = new Vector2Int(10, -3);


    #region HELPER METHODS
    public static void DisplayAllPlayerPrefs()
    {
        Debug.Log($"Difficulty: {(GenLevel)PlayerPrefs.GetInt(Difficulty_Int)}\n" +
            $"Size: {(GenSize)PlayerPrefs.GetInt(Size_Int)}\n" +
            $"XP: {PlayerPrefs.GetInt(PlXp_Int)}\n" +
            $"Bow: {PlayerPrefs.GetInt(Bow_Int)} \n" +
            $"Player name: {PlayerPrefs.GetString(PlName_Str)} \n" +
            $"Player rank: {(Ranking)PlayerPrefs.GetInt(PlRank_Int)} \n" +
            $"Wind amount: {PlayerPrefs.GetFloat(WindAmount_Fl)} \n" +
            $"Trajectory visible: {PlayerPrefs.GetInt(TrajectoryVisible_Int)} \n" +
            $"LB id: {PlayerPrefs.GetString(plLeaderBoardIdStr)} \n" +
            $"LB rank: {PlayerPrefs.GetInt(PlLeaderBoardRank_Int)} \n" +
            $"LB local score: {PlayerPrefs.GetInt(PlLeaderBoardLocalScore_Int)}");
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
        if (!PlayerPrefs.HasKey(plLeaderBoardIdStr)) PlayerPrefs.SetString(plLeaderBoardIdStr, System.Guid.NewGuid().ToString());
        return PlayerPrefs.GetString(plLeaderBoardIdStr);
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
public enum MainGameType
{
    MainMenu,
    Singleplayer,
    Multiplayer
}
public enum Ranking { Bronze, Silver, Gold, Platinum, Diamond, Champion, GrandChampion, SuperSonicLegend }
public enum SpType { Endless, Campaign, Practice }
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

