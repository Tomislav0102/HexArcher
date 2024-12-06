using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Random = UnityEngine.Random;

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

    const float CONST_ProbabilityToSpawnEmpty = 0.3f;
    const int CONST_MinNumberOfHexesInGrid = 2; 
    Vector2Int _dim = new Vector2Int(10, 10);
    ParentHex[,] _tilesInGame;
    [HideInInspector] public float scale = 0.2f;
    GenSize _size;
    
    
    public void Init()
    {
        gm = GameManager.Instance;
        containerFinal.localScale = scale * Vector3.one;
        gm.poolManager.parHexRings.localScale = scale * Vector3.one;
        _tilesInGame = new ParentHex[_dim.x, _dim.y];
        for (int i = 0; i < _dim.x * _dim.y; i++)
        {
            ParentHex hex = containerFinal.GetChild(i).GetComponent<ParentHex>();
            int x = hex.transform.GetSiblingIndex() / 10;
            int y = hex.transform.GetSiblingIndex() % 10;
            hex.pos = new Vector2Int(x, y);
            _tilesInGame[x, y] = hex;
        }


    }

    public void ChooseGrid()
    {
        if (gm.gridValuesNet.Count == 0) 
        {
            if (levelToCreate != null)
            {
                for (int i = 0; i < 100; i++)
                {
                    TileState tss =  levelToCreate.transform.GetChild(i).GetComponent<TileParent>()._tState;
                    gm.gridValuesNet.Add(0);
                    gm.gridTileStatesNet.Add((byte)tss);
                    containerFinal.GetChild(i).GetComponent<ParentHex>().HexDefinition(gm.gridTileStatesNet[i], gm.gridValuesNet[i]);
                }
            }
            else
            {
                GridRandom();
            }
        }
        else
        {
            GridUseNetworkVariables();
        }
    }


    void GridRandom()
    {
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

        int numberOfFreeHexes = 0;
        for (int i = 0; i < _dim.x; i++)
        {
            for (int j = 0; j < _dim.y; j++)
            {
                TileState tss = Random.value <= CONST_ProbabilityToSpawnEmpty ? TileState.InActive : TileState.Free;
                if (tss == TileState.Free) numberOfFreeHexes++;
                ParentHex hex = _tilesInGame[i, j];
                if (tss == TileState.Free && 
                    (hex.pos.x < sizeBorder ||
                    hex.pos.x > (9 - sizeBorder) ||
                    hex.pos.y > (9 - sizeBorder * 2))) tss = TileState.InActive;
                

                gm.gridTileStatesNet.Add((byte)tss);
                gm.gridValuesNet.Add(0);
               hex.HexDefinition((byte)tss, 0);
            }
        }

        if (numberOfFreeHexes < CONST_MinNumberOfHexesInGrid) GridRandom();
    }
    public void GridUseNetworkVariables()
    {
        for (int i = 0; i < containerFinal.childCount; i++)
        {
            containerFinal.GetChild(i).GetComponent<ParentHex>().HexDefinition(gm.gridTileStatesNet[i], gm.gridValuesNet[i]);
        }
    }
    
    #region METHOD VARIABLES
    public List<ParentHex> AllNeighbours(Vector2Int poz, bool includeTaken = false, bool includeInActive = false)
    {
        bool printDebugs = false;
        if (printDebugs) Debug.Log($"AllNeighbours at {poz}");
        bool oddRow = poz.y % 2 == 1;
        int endCoordinate = poz.x + (oddRow ? 1 : -1);
        List<ParentHex> neighbours = new List<ParentHex>();
        if (poz.x > 0) neighbours.Add(_tilesInGame[poz.x - 1, poz.y]);
        if (printDebugs) Debug.Log($"list count is {neighbours.Count} -- {poz.x - 1}, {poz.y}");
        if (poz.x < _dim.x - 1) neighbours.Add(_tilesInGame[poz.x + 1, poz.y]);
        if (printDebugs) Debug.Log($"list count is {neighbours.Count} --  {poz.x + 1}, {poz.y}");
        if (poz.y > 0)
        {
            neighbours.Add(_tilesInGame[poz.x, poz.y - 1]);
            if (printDebugs) Debug.Log($"list count is {neighbours.Count} --  {poz.x}, {poz.y - 1}");
            if (endCoordinate >= 0 && endCoordinate < _dim.x) neighbours.Add(_tilesInGame[endCoordinate, poz.y - 1]);
            if (printDebugs)  Debug.Log($"list count is {neighbours.Count} --  {endCoordinate}, {poz.y - 1}");
        }
        if (poz.y < _dim.y - 1)
        {
            neighbours.Add(_tilesInGame[poz.x, poz.y + 1]);
            if (printDebugs)  Debug.Log($"list count is {neighbours.Count} --  {poz.x}, {poz.y + 1}");
            if (endCoordinate >= 0 && endCoordinate < _dim.x) neighbours.Add(_tilesInGame[endCoordinate, poz.y + 1]);
            if (printDebugs) Debug.Log($"list count is {neighbours.Count} --  {endCoordinate}, {poz.y + 1}");

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
    const int CONST_WindScale = 20;
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
        windVector = new Vector3(newValue * CONST_WindScale * 2f, 0f, 0f);
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
    [Sirenix.OdinInspector.ReadOnly] public bool showTrajectory;
    const int CONST_LinePoints = 10;
    const float CONST_TimeBetweenPoints = 0.1f;
    const float CONST_MinY = -10;

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
        DrawProjection(spawnPoint, projectileMass, strength);
    }



    void DrawProjection(Transform spawnPoint, float projectileMass, float throwStrength)
    {
        trajectoryLr.enabled = true;
        trajectoryLr.positionCount = Mathf.CeilToInt(CONST_LinePoints / CONST_TimeBetweenPoints) + 1;
        Vector3 startPos = spawnPoint.position;
        Vector3 startVel = (throwStrength / projectileMass) * spawnPoint.forward;

        int i = 0;
        trajectoryLr.SetPosition(i, startPos);
        for (float time = 0; time < CONST_LinePoints; time += CONST_TimeBetweenPoints)
        {
            i++;
            Vector3 point = startPos + time * startVel;
            point.x = startPos.x + startVel.x * time + 0.5f * Physics.gravity.x * time * time;
            point.y = startPos.y + startVel.y * time + 0.5f * Physics.gravity.y * time * time;
            point.z = startPos.z + startVel.z * time + 0.5f * Physics.gravity.z * time * time;
            trajectoryLr.SetPosition(i, point);

            if (point.y < CONST_MinY) //this should replace floor collider
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


    LaunchData CalculateLaunchData()
    {
        float displacementY = (_targetPos.y +0.12f) - _projectilePos.y;
        float timeTotal = Mathf.Sqrt(-2 * _maxHeight / _gravity.y) + Mathf.Sqrt(2 * (displacementY - _maxHeight) / _gravity.y);

        Vector3 velY = Vector3.up * Mathf.Sqrt(- 2 * _gravity.y * _maxHeight);

        _moveX = 0.5f * _gravity.x * Mathf.Pow(timeTotal, 2);
        _moveZ = 0.5f * _gravity.z * Mathf.Pow(timeTotal, 2);
        Vector3 displacementXZ = new Vector3(_targetPos.x - _projectilePos.x - _moveX, 0f, _targetPos.z - _projectilePos.z - _moveZ);
        Vector3 velXZ = displacementXZ / timeTotal;


        return new LaunchData(velXZ + velY * (-Mathf.Sign(_gravity.y)), timeTotal);

    }


    struct LaunchData
    {
        public Vector3 initialVel;
        public float time;

        public LaunchData(Vector3 initialVel, float time)
        {
            this.initialVel = initialVel;
            this.time = time;
        }
    }

}


static class PlayerLeveling
{
    static int[] xpMilestones = new int[] { 0, 250, 300, 500, 800, 1000, 1500, 2000, 2500, 3000 };

    public static void CalculateLevelFromXp(out int lv, out int toNext)
    {
        int xp = PlayerPrefs.GetInt(Utils.Xp_Int);

        for (int i = 0; i < xpMilestones.Length; i++)
        {
            if (xp < xpMilestones[i])
            {
                lv = i;
                toNext = xpMilestones[i] - xp;
                return;
            }
        }
        lv =  xpMilestones.Length;
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
        PlayerPrefs.SetInt(Utils.Xp_Int, xpMilestones[targetLevel - 1]);
    }
    #endregion
}
[System.Serializable]
public class BowRack
{
    [SerializeField] Transform meshTr;
    public Transform spawnPoint;
    const float CONST_StartY = -1.173f;
    const int CONST_RiseSpeed = 2;
    const float CONST_FallSpeed = 0.5f;
    Tween _tRise, _tFall;

    void KillAllTweens()
    {
        if (_tRise != null && _tRise.IsActive()) _tRise.Kill();
        if (_tFall != null && _tFall.IsActive()) _tFall.Kill();
    }
    public void EnterRack(System.Action callBackAtEnd)
    {
        KillAllTweens();
       _tRise = meshTr.DOLocalMoveY(0f, CONST_RiseSpeed)
                .SetUpdate(UpdateType.Fixed)
                .OnComplete(() =>
                {
                    callBackAtEnd?.Invoke();
                });
    }
    public void HideRack()
    {
        KillAllTweens();
       _tFall = meshTr.DOLocalMoveY(CONST_StartY, CONST_FallSpeed);
    }


}
public class PlayerRegistration
{
    PlayerControl[] _players= new PlayerControl[2];
    TextMeshPro[] _displays= new TextMeshPro[2];

    public void AddPlayer(PlayerControl player, bool isHost)
    {
        int index = isHost ? 0 : 1;
        _players[index] = player;
        _displays[index] = _players[index].displayNameTr.GetComponent<TextMeshPro>();
        // string myName = "";
        // string levelDisplay = "Level - ";
        // string rankDisplay = "Leaderboard - ";
        // if (isHost)
        // {
        //     myName = GameManager.Instance.nameBlueNet.Value.ToString();
        //     levelDisplay += GameManager.Instance.leveBlueNet.Value.ToString();
        //     if (GameManager.Instance.leaderboardBlueNet.Value < 0) rankDisplay = string.Empty;
        //     else
        //     {
        //         int rank = GameManager.Instance.leaderboardBlueNet.Value;
        //         rankDisplay += (rank + 1).ToString();
        //     }
        // }
        // else
        // {
        //     myName = GameManager.Instance.nameRedNet.Value.ToString();
        //     levelDisplay += GameManager.Instance.leveRedNet.Value.ToString();
        //     if (GameManager.Instance.leaderboardRedNet.Value < 0) rankDisplay = string.Empty;
        //     else
        //     {
        //         int rank = GameManager.Instance.leaderboardRedNet.Value;
        //         rankDisplay += (rank + 1).ToString();
        //     }
        //
        // }
        // if(_displays[index] != null) _displays[index].text = myName + "\n" + levelDisplay+ "\n" + rankDisplay;
    }
    public void RemovePlayer(PlayerControl player)
    {
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] == player) _players[i] = null;
        }
    }


    public void FillDisplay()
    {
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] == null) continue;
            string myName = "";
            string levelDisplay = "Level - ";
            string rankDisplay = "Leaderboard - ";
            if (i == 0)
            {
                myName = GameManager.Instance.nameBlueNet.Value.ToString();
                levelDisplay += GameManager.Instance.leveBlueNet.Value.ToString();
                if (GameManager.Instance.leaderboardBlueNet.Value < 0) rankDisplay = string.Empty;
                else
                {
                    int rank = GameManager.Instance.leaderboardBlueNet.Value;
                    rankDisplay += (rank + 1).ToString();
                }
            }
            else
            {
                myName = GameManager.Instance.nameRedNet.Value.ToString();
                levelDisplay += GameManager.Instance.leveRedNet.Value.ToString();
                if (GameManager.Instance.leaderboardRedNet.Value < 0) rankDisplay = string.Empty;
                else
                {
                    int rank = GameManager.Instance.leaderboardRedNet.Value;
                    rankDisplay += (rank + 1).ToString();
                }

            }
            _displays[i].text = myName + "\n" + levelDisplay+ "\n" + rankDisplay;
            _players[i].name = $"Igrach {_players[i].GetComponent<NetworkObject>().OwnerClientId}";
        }
    }
    public bool HasSecondPlayer() => _players[1] != null;

    public void GameOver()
    {
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] != null)
            {
                Utils.DeActivateGo(_players[i].bowControl.gameObject);
            }
        }
    }
    int TargetPlayerIndex(PlayerControl playerOpposing)
    {
        int targetPlayerIndex = 0;
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] == playerOpposing) targetPlayerIndex = (i + 1) % 2;
        }
        return targetPlayerIndex;
    }
}

