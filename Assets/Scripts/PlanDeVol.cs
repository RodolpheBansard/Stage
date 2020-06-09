using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanDeVol : MonoBehaviour
{
    public GameObject planeBody;
    public OnlineMaps om;
    [Range(0.0f, 90.0f)] public float lat = 90;
    [Range(0.0f, 90.0f)] public float longi = 90;


    private void Start()
    {
        StartCoroutine(Fly());
    }

    IEnumerator Fly()
    {
        while (true)
        {
            lat -= 0.001f;
            om.SetLatitude(lat);
            om.SetLongitude(longi);
            yield return new WaitForSeconds(0.01f);
        }        
    }
}
