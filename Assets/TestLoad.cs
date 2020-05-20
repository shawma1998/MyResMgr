using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class TestLoad : MonoBehaviour
{

    string path;
    string mainpath;
    public Slider slider;
    int j = 0;
    float start = 0;
    Coroutine a;
    AssetBundleManifest abm;
    // Use this for initialization
    void Start()
    {
        print("本次加载1000个资源");
        start = Time.time;
        for (int i = 0; i <1000; i++)
        {

            int ramdomIndex = UnityEngine.Random.Range(1, 5);
            float ramdomLevel = UnityEngine.Random.Range(1, 3);
            RequestLevel level = ramdomLevel == 1 ? RequestLevel.High : RequestLevel.Low;

            LoadRes(ramdomIndex, level);
        }

        
        //ResourceManager.Instance.GetResource("cube.u3d", "Cube", (obj) => {
        //    Instantiate(obj);
        //});

        //for (int i = 0; i < 100; i++)
        //{
        //    LoadRes(1, RequestLevel.High);
        //}
    }

    

    void LoadRes(int abIndex, RequestLevel level)
    {
        ResourceManager.Instance.GetResource("u3d"+ abIndex + ".u3d", "obj", (obj) => {
            j++;
            print("----->" + obj.name);
            //Instantiate(obj);

            if(j == 999)
            {
                print("花费时间-------"+(Time.time - start).ToString());
            }
        }, level);
    }

    void ShowLevelDep(string rootABName)
    {
        print("root------>" + rootABName);
        string[] dependencies = abm.GetDirectDependencies(rootABName);
        if (dependencies.Length == 0) return;
        for (int i = 0; i < dependencies.Length; i++)
        {
            ShowLevelDep(dependencies[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

   

   
}
