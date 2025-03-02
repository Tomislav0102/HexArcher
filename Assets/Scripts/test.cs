using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class test : MonoBehaviour
{
  public Transform head;
  public Material mat;
  void Start()
  {
    MeshRenderer mRend = head.GetComponent<MeshRenderer>();
    Material[] mats = mRend.materials;
    mats[1] = mat;
    mRend.materials = mats;

  }
}