using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

public class CreateAssetBundles {

    [MenuItem("AssetBundles/Build AssetBundles")] //特性
    static void BuildAssetBundle()
    {
        string dir = Application.streamingAssetsPath+"/AssetBundles"; //相对路径
        if (!Directory.Exists(dir))   //判断是否存在
        {
            Directory.CreateDirectory(dir);
        }
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

    }
}
