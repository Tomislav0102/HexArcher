using UnityEngine;
using Unity.Netcode;

//[CreateAssetMenu]
public class SoPlayerData : ScriptableObject
{
   // public string myName;
   // public string myLevel;
   // public string myAutheticationId;
  //  public int myLeaderboardRank;
    public PlayerColor playerColor;
    public NetworkObject netObj;
    public ulong playerId;
    public PlayerControl playerControl;
    public Color colMain;
    public Gradient colGradientTrail;
    public Material matMain;
    public Material[] matsHex;

}
