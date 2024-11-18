using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
//using Firebase.Firestore;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

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
        List<int> brojevi = Enumerable.Range(0, size).ToList();
        var rnd = new System.Random();
        var randNums = brojevi.OrderBy(n => rnd.Next());
        List<int> list = new List<int>();
        foreach (var item in randNums)
        {
            list.Add(item);
        }
        return list;
    }
    public static List<T> RandomListByType<T>(List<T> pocetna)
    {
        var rnd = new System.Random();
        var randNums = pocetna.OrderBy(n => rnd.Next());
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
    Transform MyMainTransform { get; set; } //used for parenting arrow
    void HitMe(PlayerColor arrowColor);
}
#endregion

#region CLASSES
public class RelayManager
{
    int _maxPlayers;
    public RelayManager(int maxPlayers)
    {
        _maxPlayers = maxPlayers;
    }
    
    public async Task<Allocation> AllocateRelay() //called by host
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_maxPlayers - 1); //host is excluded
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to allocate relay: " + e.Message);
            return default;
        }
    }
    
    public async Task<string> GetRelayJoinCode(Allocation allocation) //called by host
    {
        try
        {
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to get relay join code: " + e.Message);
            return default;
        }
    }
    
    public async Task<JoinAllocation> JoinRelay(string relayJoinCode) //called by client
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to join relay: " + e.Message);
            return default;
        }
    } 
}
public struct RelayHostData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public System.Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] Key;
}
public struct RelayJoinData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public System.Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] HostConnectionData;
    public byte[] Key;
}

[System.Serializable]
public class GridManager
{
    GameManager gm;
    [SerializeField] GameObject levelToCreate;
    [SerializeField] Transform containerFinal;

    Vector2Int _dim = new Vector2Int(10, 10);
    ParentHex[,] _tilesInGame;
    [HideInInspector] public float scale = 0.2f;
    GenSize _size;

    public void Init()
    {
        SharedInit();
        GridInitialization();
    }
    void SharedInit()
    {
        gm = GameManager.Instance;
        containerFinal.localScale = scale * Vector3.one;
        gm.poolManager.parHexRings.localScale = scale * Vector3.one;
    }



    void GridInitialization()
    {
        _tilesInGame = new ParentHex[_dim.x, _dim.y];
        for (int i = 0; i < _dim.x * _dim.y; i++)
        {
            ParentHex hex = containerFinal.GetChild(i).GetComponent<ParentHex>();
            int x = hex.transform.GetSiblingIndex() / 10;
            int y = hex.transform.GetSiblingIndex() % 10;
            hex.pos = new Vector2Int(x, y);
            _tilesInGame[x, y] = hex;
        }

        if (levelToCreate != null)
        {
            for (int i = 0; i < 100; i++)
            {
                TileState tss =  levelToCreate.transform.GetChild(i).GetComponent<TileParent>()._tState;
                gm.hexValNet.Add(0);
                gm.hexStateNet.Add((byte)tss);
                containerFinal.GetChild(i).GetComponent<ParentHex>().Tstate = tss;

            }
            return;
        }

        _size = (GenSize)PlayerPrefs.GetInt(Utils.Size_Int);
        int sizeBorder = 0;
        switch (_size)
        {
            case GenSize.Small:
                sizeBorder = 3;
                break;
            case GenSize.Medium:
                sizeBorder = 2;
                break;
            case GenSize.Big:
                sizeBorder = 0;
                break;
        }

        for (int i = 0; i < _dim.x; i++)
        {
            for (int j = 0; j < _dim.y; j++)
            {
                TileState tss = Random.value <= 0.7f ? TileState.Free : TileState.InActive;
                ParentHex hex = _tilesInGame[i, j];
                if (hex.pos.x < sizeBorder ||
                    hex.pos.x > (9 - sizeBorder) ||
                    hex.pos.y > (9 - sizeBorder * 2)) tss = TileState.InActive;
                hex.startTs = tss;

                gm.hexStateNet.Add((byte)tss);
                gm.hexValNet.Add(0);
            }
        }
    }
    public void GridLateJoin()
    {
        SharedInit();
        for (int i = 0; i < containerFinal.childCount; i++)
        {
            containerFinal.GetChild(i).GetComponent<ParentHex>().ClientLateJoin(gm.hexStateNet[i], gm.hexValNet[i]);
        }
    }

    #region METHOD VARIABLES
    public List<ParentHex> AllNeighbours(Vector2Int poz, bool includeTaken = false, bool includeInActive = false)
    {
        bool oddRow = poz.y % 2 == 1;
        int endCoor = poz.x + (oddRow ? 1 : -1);
        List<ParentHex> neighbours = new List<ParentHex>();

        if (poz.x > 0) neighbours.Add(_tilesInGame[poz.x - 1, poz.y]);
        if (poz.x < _dim.x - 1) neighbours.Add(_tilesInGame[poz.x + 1, poz.y]);
        if (poz.y > 0)
        {
            neighbours.Add(_tilesInGame[poz.x, poz.y - 1]);
            if (endCoor >= 0 && endCoor < _dim.x) neighbours.Add(_tilesInGame[endCoor, poz.y - 1]);
        }
        if (poz.y < _dim.y - 1)
        {
            neighbours.Add(_tilesInGame[poz.x, poz.y + 1]);
            if (endCoor >= 0 && endCoor < _dim.x) neighbours.Add(_tilesInGame[endCoor, poz.y + 1]);

        }

        List<ParentHex> modifiedList = new List<ParentHex>();
        foreach (ParentHex item in neighbours)
        {
            switch (item.Tstate)
            {
                case TileState.Free:
                    modifiedList.Add(item);
                    break;
                case TileState.InActive:
                    if (includeInActive) modifiedList.Add(item);
                    break;
                case TileState.Taken:
                    if (includeTaken) modifiedList.Add(item);
                    break;
            }
        }

        return modifiedList;
    }

    public List<ParentHex> AllTilesByType(TileState ts)
    {
        List<ParentHex> li = new List<ParentHex>();
        foreach (ParentHex item in _tilesInGame)
        {
            if (item.Tstate == ts) li.Add(item);
        }
        return li;
    }

    public int NumOfTilesByType(TileState ts)
    {
        int count = 0;
        for (int i = 0; i < _dim.x; i++)
        {
            for (int j = 0; j < _dim.y; j++)
            {
                if (_tilesInGame[i, j].Tstate == ts) count++;
            }
        }

        return count;
    }
    #endregion

}

[System.Serializable]
public class PoolManager 
{
    public Transform parHexRings;
    RingsPooled[] _hexRings;
    int _counterHexRings;

    public void Init()
    {
        _hexRings = Utils.AllChildren<RingsPooled>(parHexRings);
    }

    public RingsPooled GetHexRing() => GetGenericObject<RingsPooled>(_hexRings, ref _counterHexRings);

    T GetGenericObject<T>(T[] arr, ref int count, int miliSecondsDelayEnd = 0)
    {
        T obj = arr[count];
        count = (1 + count) % arr.Length;

        if (miliSecondsDelayEnd > 0) End(obj, miliSecondsDelayEnd);
        return obj;
    }
    async void End<T>(T tip, int miliSecondsDelay)
    {
        await Task.Delay(miliSecondsDelay);
        if (tip.GetType() == typeof(GameObject))
        {
            GameObject go = tip as GameObject;
            go.SetActive(false);
        }
        else if (tip.GetType() == typeof(Transform))
        {
            Transform tr = tip as Transform;
            tr.gameObject.SetActive(false);
        }
        else if (tip.GetType() == typeof(ParticleSystem))
        {
            ParticleSystem ps = tip as ParticleSystem;
            ps.Stop();
        }
        else if (tip.GetType() == typeof(LineRenderer))
        {
            LineRenderer lr = tip as LineRenderer;
            lr.enabled = false;
        }
    }
}

[System.Serializable]
public class WindManager
{
    public Vector3 gravityVector;
    [Sirenix.OdinInspector.ReadOnly] public Vector3 windVector;
    const int CONST_WINDSCALE = 20;
    [SerializeField] TextMeshProUGUI displayWindValue;
    [SerializeField] Transform windIcon;
    [SerializeField] Cloth flagCloth;

    
    public void WindChange(float previousValue, float newValue)
    {
        if (Mathf.Abs(newValue) < 0.1f)
        {
            windIcon.localEulerAngles = Vector3.zero;
            flagCloth.damping = 0.3f;
        }
        else
        {
            int a = newValue < 0f ? -1 : 1;
            windIcon.localEulerAngles = -a * 90f * Vector3.forward;
            flagCloth.damping = 0f;
        }
        windVector = new Vector3(newValue * CONST_WINDSCALE * 2f, 0f, 0f);
        displayWindValue.text = $"{Mathf.Abs(windVector.x).ToString("F0")} km/h";
        flagCloth.externalAcceleration = windVector;
        Physics.gravity = gravityVector + windVector;
    }

}

[System.Serializable]
public class DrawTrajectory
{
    [SerializeField] LineRenderer trajectoryLr;
    [SerializeField] Gradient[] colorsTrajectory = new Gradient[2];
    /*[Sirenix.OdinInspector.ReadOnly]*/ public bool showTrajectory;
    readonly int _linePoints = 10;
    readonly float _timeBetweenPoints = 0.1f;
    const float CONST_MINY = -10;

    public void Trajectory(bool draw)
    {
        if (!draw || !showTrajectory)
        {
            trajectoryLr.enabled = false; 
        }
    }
    public void Trajectory(Transform spawnPoint, PlayerColor playerActive, float projectileMass, float strength, bool draw = true)
    {
        if (!draw || !showTrajectory)
        {
            trajectoryLr.enabled = false;
            return;
        }
        trajectoryLr.colorGradient = colorsTrajectory[(int)playerActive];
        DrawPojection(spawnPoint, projectileMass, strength);
    }



    void DrawPojection(Transform spawnPoint, float projectileMass, float throwStrength)
    {
        trajectoryLr.enabled = true;
        trajectoryLr.positionCount = Mathf.CeilToInt(_linePoints / _timeBetweenPoints) + 1;
        Vector3 startPos = spawnPoint.position;
        Vector3 startVel = (throwStrength / projectileMass) * spawnPoint.forward;

        int i = 0;
        trajectoryLr.SetPosition(i, startPos);
        for (float time = 0; time < _linePoints; time += _timeBetweenPoints)
        {
            i++;
            Vector3 point = startPos + time * startVel;
            point.x = startPos.x + startVel.x * time + 0.5f * Physics.gravity.x * time * time;
            point.y = startPos.y + startVel.y * time + 0.5f * Physics.gravity.y * time * time;
            point.z = startPos.z + startVel.z * time + 0.5f * Physics.gravity.z * time * time;
            trajectoryLr.SetPosition(i, point);

            if (point.y < CONST_MINY) //this should replace floor collider
            {
                trajectoryLr.positionCount = i;
                return;
            }

            Vector3 lastPosition = trajectoryLr.GetPosition(i - 1);
            if (Physics.Raycast(lastPosition, (point - lastPosition).normalized, out RaycastHit hit, (point - lastPosition).magnitude, GameManager.Instance.layForTrajectory))
            {
                trajectoryLr.SetPosition(i, hit.point);
                trajectoryLr.positionCount = i + 1;
                return;
            }
        }
    }
}
public class LaunchVelocity 
{
    Vector3 _projectilePos, _targetPos, _gravity;
    float _maxHeight = 25f;
  //  LineRenderer _lr;
    float _moveX, _moveZ;

    public Vector3 Vel(Vector3 projectile, Vector3 tar, Vector3 gravityWind)
    {
        _projectilePos = projectile;
        _targetPos = tar;
        _gravity = gravityWind;

        if (_gravity.y < 0) _maxHeight = Mathf.Max(_projectilePos.y, _targetPos.y) + 0.5f;
        else _maxHeight = Mathf.Min(_projectilePos.y, _targetPos.y) - 0.5f;

        return CalculateLaunchData().initialVel;
    }


    LauncData CalculateLaunchData()
    {
        float displacementY = (_targetPos.y +0.12f) - _projectilePos.y;
        float timeTotal = Mathf.Sqrt(-2 * _maxHeight / _gravity.y) + Mathf.Sqrt(2 * (displacementY - _maxHeight) / _gravity.y);

        Vector3 velY = Vector3.up * Mathf.Sqrt(- 2 * _gravity.y * _maxHeight);

        _moveX = 0.5f * _gravity.x * Mathf.Pow(timeTotal, 2);
        _moveZ = 0.5f * _gravity.z * Mathf.Pow(timeTotal, 2);
        Vector3 displacementXZ = new Vector3(_targetPos.x - _projectilePos.x - _moveX, 0f, _targetPos.z - _projectilePos.z - _moveZ);
        Vector3 velXZ = displacementXZ / timeTotal;


        return new LauncData(velXZ + velY * (-Mathf.Sign(_gravity.y)), timeTotal);

    }


    struct LauncData
    {
        public Vector3 initialVel;
        public float time;

        public LauncData(Vector3 initialVel, float time)
        {
            this.initialVel = initialVel;
            this.time = time;
        }
    }

}

[System.Serializable]
class PowerUpsManager 
{
    [SerializeField] PowerUp powerUpPrefab; //no pooling
    List<PowerUp> _allPowerUps = new List<PowerUp>();
    [SerializeField] Transform[] spawnPoints;

    public void Init()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPoints[i].LookAt(1.5f * Vector3.up);
            _allPowerUps.Add(GameObject.Instantiate(powerUpPrefab, spawnPoints[i].position, spawnPoints[i].rotation, spawnPoints[i]));
            RdnMove(i);
        }
    }
    
    void RdnMove(int ordinal)
    {
        float moveSpeed = 1f;
        float spread = 10f;
        Vector3 rdn = Random.insideUnitCircle;
        _allPowerUps[ordinal].MyMainTransform.DOLocalMove(spread * rdn, moveSpeed)
            .SetSpeedBased(true)
            .OnComplete(() =>
            {
                RdnMove(ordinal);
            });
    }
}

static class PlayerLeveling
{
    static int[] _xpMilestones = new int[] { 0, 250, 300, 500, 800, 1000, 1500, 2000, 2500, 3000 };

    public static void CalculateLevelFromXp(out int lv, out int toNext)
    {
        int xp = PlayerPrefs.GetInt(Utils.Xp_Int);

        for (int i = 0; i < _xpMilestones.Length; i++)
        {
            if (xp < _xpMilestones[i])
            {
                lv = i;
                toNext = _xpMilestones[i] - xp;
                return;
            }
        }
        lv =  _xpMilestones.Length;
        toNext = 0;
    }

    public static void AddToXp(GenFinish finishType)
    {
        float diffMod = Mod(PlayerPrefs.GetInt(Utils.Difficulty_Int));
        float sizeMod = Mod(PlayerPrefs.GetInt(Utils.Size_Int));
        
        Vector3Int xpWinDrawLoseSp = new Vector3Int(100, 50, 50);
        Vector3Int xpWinDrawLoseMp = new Vector3Int(300, 100, 100);
        Vector3Int spOrMp = Utils.GameType == MainGameType.Singleplayer ? xpWinDrawLoseSp : xpWinDrawLoseMp;
        
        int wld = 0;
        switch (finishType)
        {
            case GenFinish.Win:
                wld = spOrMp.x;
                break;
            case GenFinish.Lose:
                wld = spOrMp.y;
                break;
            case GenFinish.Draw:
                wld = spOrMp.z;
                break;
        }
        
        int final = PlayerPrefs.GetInt(Utils.Xp_Int) + (int)(diffMod * sizeMod * wld);
        PlayerPrefs.SetInt(Utils.Xp_Int, final);
        Utils.PlayerXpUpdated?.Invoke();
        
        float Mod(int rank) => 1 + rank * 0.5f;
    }
    #region DEBUG
    public static void GetMeToLevel(int targetLevel)
    {
        CalculateLevelFromXp(out int lv, out int toNext);
        if(targetLevel <= lv || targetLevel > 10) return;
        PlayerPrefs.SetInt(Utils.Xp_Int, _xpMilestones[targetLevel - 1]);
    }
    #endregion
}
[System.Serializable]
public class BowRack
{
    [SerializeField] Transform meshTr;
    public Transform spawnPoint;
    const float CONST_STARTY = -1.173f;
    const int CONST_RISESPEED = 2;
    const float CONST_FALLSPEED = 0.5f;
    Tween _tRise, _tFall;

    void KillAllTweens()
    {
        if (_tRise != null && _tRise.IsActive()) _tRise.Kill();
        if (_tFall != null && _tFall.IsActive()) _tFall.Kill();
    }
    public void EnterRack(System.Action callBackAtEnd)
    {
        KillAllTweens();
       _tRise = meshTr.DOLocalMoveY(0f, CONST_RISESPEED)
                .SetUpdate(UpdateType.Fixed)
                .OnComplete(() =>
                {
                    callBackAtEnd?.Invoke();
                });
    }
    public void HideRack()
    {
        KillAllTweens();
       _tFall = meshTr.DOLocalMoveY(CONST_STARTY, CONST_FALLSPEED);
    }

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
