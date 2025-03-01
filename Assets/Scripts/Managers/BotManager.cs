using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class BotManager : MonoBehaviour
{
    GameManager gm;

    bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
           // print($"bot {value}");
            gm.uImanager.displayStartInfo.text = "";
            if (value)
            {
                Utils.Activation(bow, true);
            }
            else
            {
                Utils.Activation(bow, false);
                arrowSpawnPoint.eulerAngles = Vector3.zero;
            }
        }
    }
    bool _isActive;

    [SerializeField] GameObject bow;

    #region CHOOSING BEST HEX
    [ShowInInspector][ReadOnly] List<ParentHex> _finalListForBot = new List<ParentHex>(); //all hexes sorted by their value
    [ShowInInspector][ReadOnly] ParentHex _target; //chosen hex to target
    int[] _maxNumOfBestHexes = new int[] { 10, 3, 1 };
    #endregion

    #region SHOOTING AT CHOSEN HEX
    float Precision()
    {
        switch (gm.difficultyNet.Value)
        {
            case GenDifficulty.Easy:
                return Random.value + (Random.value > 0.5 ? 0.3f : 0f);
            case GenDifficulty.Normal:
                return Mathf.Pow(Random.value, 2);
            case GenDifficulty.Hard:
                return Mathf.Pow(Random.value, 3);
            default:
                return 0;
        }
    }
    [SerializeField] Transform arrowSpawnPoint, targetPointer;
    LaunchVelocity _launchVelocity = new LaunchVelocity();
    [SerializeField] AudioSource audioSource;
    Tween _tweenAim;
    #endregion

    private void Awake()
    {
        gm = GameManager.Instance;
    }

    private void OnEnable()
    {
        gm.playerTurnNet.OnValueChanged += NetVarEv_NextTurn;
        Utils.GameStarted += () => IsActive = true;
    }
    private void OnDisable()
    {
        gm.playerTurnNet.OnValueChanged -= NetVarEv_NextTurn;
        Utils.GameStarted -= () => IsActive = true;
    }

    public void EndBot() => IsActive = false;

    private void NetVarEv_NextTurn(PlayerColor previousValue, PlayerColor newValue)
    {
        if (!IsActive || newValue == PlayerColor.Blue) return;
        BotMethod();
    }



    void BotMethod()
    {
        List<ParentHex> allFree = gm.gridManager.AllTilesByType(TileState.Free);
        if (allFree.Count == 0) return;

        _finalListForBot = HexSortingByTargetValue(allFree);

        int finalCount = Mathf.Min(_finalListForBot.Count, _maxNumOfBestHexes[(int)gm.difficultyNet.Value]);

        _target = _finalListForBot[Random.Range(0, finalCount)];
        Vector2 rdn = Random.insideUnitCircle * Precision() * gm.gridManager.scale * 10f;
        Vector3 tarPos = new Vector3(_target.center.position.x + rdn.x, _target.center.position.y + rdn.y, _target.center.position.z);
        targetPointer.position = tarPos;
        Vector3 vel = _launchVelocity.Vel(arrowSpawnPoint.position, targetPointer.position, gm.windManager.gravityVector + gm.windManager.windVector);
        _tweenAim = arrowSpawnPoint.DORotate(Quaternion.LookRotation(vel.normalized).eulerAngles, 2f)
                                 .SetEase(Ease.OutElastic)
                                 .OnComplete(() =>
                                 {
                                     gm.SpawnRealArrow(arrowSpawnPoint.position, arrowSpawnPoint.rotation);
                                     gm.arrowReal.Release(vel);
                                     gm.audioManager.PlayOnMyAudioSource(audioSource, gm.audioManager.bowRelease);
                                 });
    }

    List<ParentHex> HexSortingByTargetValue(List<ParentHex> allFree)
    {
        //first reset all the values
        foreach (ParentHex item in allFree)
        {
            item.valueForBot = 0;
        }

        //all hexes are assigned a value that derives from their own and their neighbours 'CurrentValue' variable
        foreach (ParentHex item in allFree)
        {
            item.valueForBot += -10 + item.CurrentValue;
            List<ParentHex> neigh = gm.gridManager.AllNeighbours(item.pos);
            foreach (ParentHex n in neigh)
            {
                item.valueForBot += -10 + n.CurrentValue;
            }
        }

        return allFree.OrderByDescending(n => n.valueForBot).ToList();

        // //list of 'ParentHex' monobehaviours is sorted by its variable 'valueForBot'
        // List<ParentHex> final = new List<ParentHex>();
        // final.Add(allFree[0]);
        // for (int i = 1; i < allFree.Count; i++)
        // {
        //     ParentHex item = allFree[i];
        //     if (item.valueForBot > final[final.Count - 1].valueForBot) final.Add(item);
        //     else FindSpotForItem(item);
        // }
        // return final;
        //
        // void FindSpotForItem(ParentHex hex)
        // {
        //     for (int i = 0; i < final.Count; i++)
        //     {
        //         if (final[i].valueForBot >= hex.valueForBot)
        //         {
        //             final.Insert(i, hex);
        //             return;
        //         }
        //     }
        // }

    }
}
