using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridManager
{
    GameManager gm;
    [SerializeField] Transform containerFinal;

    const float CONST_ProbabilityToSpawnEmpty = 0.3f;
    const int CONST_MinNumberOfHexesInGrid = 2; 
    Vector2Int _dim = new Vector2Int(11, 11);
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
            int x = hex.transform.GetSiblingIndex() / _dim.x;
            int y = hex.transform.GetSiblingIndex() % _dim.y;
            hex.pos = new Vector2Int(x, y);
            _tilesInGame[x, y] = hex;
        }


    }

    public void ChooseGrid(GameObject levelToCreate = null)
    {
        if (gm.gridValuesNet.Count == 0)
        {
            if (levelToCreate != null)
            {
                for (int i = 0; i < _dim.x * _dim.y; i++)
                {
                    TileState tss = levelToCreate.transform.GetChild(i).GetComponent<TileParent>()._tState;
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
                    hex.pos.x > (_dim.x - 1 - sizeBorder) ||
                    hex.pos.y > (_dim.y - 1 - sizeBorder * 2))) tss = TileState.InActive;
                

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