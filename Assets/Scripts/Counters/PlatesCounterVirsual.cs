using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatesCounterVirsual : MonoBehaviour
{
    [SerializeField] private PlatesCounter platesCounter;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private Transform plateVirsualPrefab;

    private List<GameObject> plateVisualGameObjectList;

    private void Awake()
    {
        plateVisualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        platesCounter.OnPlateSpawned += PlateCounter_OnPlateSpawned;
        platesCounter.OnPlateRemoved += PlateCounter_OnPlateRemoved;

    }

    private void PlateCounter_OnPlateRemoved(object sender, EventArgs e)
    {
        GameObject plateGameObject = plateVisualGameObjectList[plateVisualGameObjectList.Count - 1];
        plateVisualGameObjectList.Remove(plateGameObject);
        Destroy(plateGameObject);
    }

    private void PlateCounter_OnPlateSpawned(object sender, EventArgs e)
    {
        Transform plateVirsualTransform = Instantiate(plateVirsualPrefab, counterTopPoint);
        float plateOffsetY = .1f;
        //先用count再加
        plateVirsualTransform.localPosition = new Vector3(0, plateOffsetY * plateVisualGameObjectList.Count, 0);

        plateVisualGameObjectList.Add(plateVirsualTransform.gameObject);
    }

}
