using UnityEngine;
using Unity.Netcode;

//[CreateAssetMenu]
public class SoFactionData : ScriptableObject
{
    public PlayerColor playerColor;
    public Color colMain;
    public Gradient colGradientTrail;
    public Material matMain;
    public Material[] matsHex;

}
