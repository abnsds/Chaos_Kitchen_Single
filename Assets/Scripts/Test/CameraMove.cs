using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;
    [SerializeField] private float OffsetZ;
    [SerializeField] private float OffsetY;


    private void Update()
    {
        
    }
    private void SetCameraPosition()
    {
        
        Vector3 playerPos = RectangleMapGenerator.GetInstance().GetPlayerSpawnPosition();
        //Vector3 cameraPosition = new Vector3()
    }
}
