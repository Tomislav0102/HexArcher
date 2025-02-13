using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Used for level generation only and only for editor
/// Has a 'ISerializationCallbackReceiver' beacuse 2D arrays cant serialize
/// Can generate, randomize and save exisitng grid. Grid is saved as a prefab.
/// </summary>
public class LayoutGenerator : MonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField] Transform positionCarrier;
    [SerializeField] GameObject prefabTile;
    [Title("Grid setup")]
    [SerializeField] Vector2Int dim;
    [SerializeField] float size = 1f;
    [PropertySpace(SpaceBefore = 0, SpaceAfter = 20)]
    [SerializeField] float offsetPosition = 0.1f;
    [SerializeField] public Marker[,] markersInEditor; //has data that will be used by 'ParentHex' monobehaviour when creating it
    bool IsGenerated() => positionCarrier.childCount > 0;

    #region SERIALIZATION
    [SerializeField]
    [HideInInspector] List<Package<Marker>> markersInPackage;
    [System.Serializable]
    public struct Package<T>
    {
        public int index0;
        public int index1;
        public T ele;

        public Package(int index0, int index1, T ele)
        {
            this.index0 = index0;
            this.index1 = index1;
            this.ele = ele;
        }
    }

    public void OnBeforeSerialize()
    {
        markersInPackage = new List<Package<Marker>>();
        for (int i = 0; i < markersInEditor.GetLength(0); i++)
        {
            for (int j = 0; j < markersInEditor.GetLength(1); j++)
            {
                markersInPackage.Add(new Package<Marker>(i, j, markersInEditor[i, j]));
            }
        }
    }

    public void OnAfterDeserialize()
    {
        markersInEditor = new Marker[dim.x, dim.y];
        foreach (var item in markersInPackage)
        {
            markersInEditor[item.index0, item.index1] = item.ele;
        }
    }

    #endregion


#if (UNITY_EDITOR)

     [GUIColor("green")]
     [BoxGroup("General controls", order: 0f)]
     [Button(ButtonSizes.Large)]
    void GenerateGrid()
    {
        DestroyAll();
        markersInEditor = new Marker[dim.x, dim.y];
        offsetPosition += 1f;
        positionCarrier.localPosition = Vector3.zero;
        float offX = 0.5f * Mathf.Sqrt(3);
        for (int i = 0; i < dim.x; i++)
        {
            for (int j = 0; j < dim.y; j++)
            {
                Vector2 position = new Vector2(i * offX, j * 0.75f) * offsetPosition;
                if (j % 2 == 1) position.x += offsetPosition * 0.5f * offX;
                GameObject go = Instantiate(prefabTile, position, Quaternion.identity, positionCarrier);
                go.name = $"Marker ({i}-{j})";
                go.transform.position += new Vector3(0.5f, 0.5f, 0f);
                markersInEditor[i,j] = go.GetComponent<Marker>();
                markersInEditor[i, j].pos = new Vector2Int(i, j);
            }
        }

        transform.localScale = new Vector3(size, size, 1f);
        offsetPosition -= 1f;
    }
     [BoxGroup("General controls", order: 0)]
     [Button(ButtonSizes.Large)]
     [GUIColor("green")]
    void DestroyAll()
    {
        List<GameObject> gos = new List<GameObject>();
        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        {
            gos.Add(transform.GetChild(0).GetChild(i).gameObject);
        }
        for (int i = 0; i < gos.Count; i++)
        {
            DestroyImmediate(gos[i]);
        }
        positionCarrier.localPosition = Vector3.zero;
    }
    [BoxGroup("Controls for randomization",order: 0.1f)]
    [SerializeField]
    [ShowIf("IsGenerated")]
    [Range(0.2f, 1f)]
    [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
    [Tooltip("Density of active tiles. 1 means full grid, no inactives.")]
    float density = 0.5f;
     [BoxGroup("Controls for randomization", order: 0.1f)]
     [Button]
     [ShowIf("IsGenerated")]
     [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
    void RandomLevel()
    {
        for (int i = 0; i < dim.x; i++)
        {
            for (int j = 0; j < dim.y; j++)
            {
                markersInEditor[i, j].Tstate = Random.value <= density ? TileState.Free : TileState.InActive;
            }
        }

    }
    [GUIColor("orange")]
    [ShowIf("IsGenerated")]
    [BoxGroup("Saving map", order: 0.2f)]
     [SerializeField] string levelName = "Level X";
    [ShowIf("IsGenerated")]
    [Button]
    [GUIColor("orange")]
    [BoxGroup("Saving map", order: 0.2f)]
    void SaveMap()
    {
        string localPath = $"Assets/Prefabs/Levels/{levelName}.prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        GameObject go = Instantiate(positionCarrier.gameObject);
        for (int i = 0; i < go.transform.childCount; i++)
        {
            go.transform.GetChild(i).GetComponent<Marker>().done = true;
        }
        PrefabUtility.SaveAsPrefabAsset(go, localPath, out bool success);
        if (success) print($"{levelName} has been saved successfully");
        else Debug.LogError("Error occured, level was not saved");
        DestroyImmediate(go);
        DestroyAll();
    }
#endif
}
