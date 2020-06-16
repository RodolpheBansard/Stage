using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Benchmark : MonoBehaviour
{
    [SerializeField] List<GameObject> terrains = new List<GameObject>();
    [SerializeField] Text terrainName;
    [SerializeField] Text fpsText;
    private int index = 0;
    private float deltaTime = 0;

    void Start()
    {
        foreach (GameObject terrain in terrains)
        {
            terrain.SetActive(false);
        }
        terrains[index].SetActive(true);
        terrainName.text = terrains[index].name;
    }

    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString();
    }

    public void LoadNextTerrain()
    {
        print("clicked");
        terrains[index].SetActive(false);

        index++;
        if (index >= terrains.Count)
        {
            index = 0;
        }

        terrains[index].SetActive(true);
        terrainName.text = terrains[index].name;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
