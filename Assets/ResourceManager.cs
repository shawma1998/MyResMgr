using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public enum RequestLevel
{
    Low,
    High
}

public class ResourceManager : MonoBehaviour {

    /// <summary>
    /// 允许每帧检查多少次队列——快加载队列
    /// </summary>
    public const int MAX_CHECK_INFRAME_HIGH = 5;
    /// <summary>
    /// 允许每帧检查多少次队列——慢加载队列
    /// </summary>
    public const int MAX_CHECK_INFRAME_LOW = 15;

    public static ResourceManager Instance;

    
    private AssetBundleManifest manifest;
    public string fileMain;

    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        fileMain = Application.streamingAssetsPath + "/AssetBundles/";

        AssetBundle manifestAB = AssetBundle.LoadFromFile(fileMain+ "AssetBundles");  // 加载总ManifestAssetBundle
        manifest = (AssetBundleManifest)manifestAB.LoadAsset("AssetBundleManifest");

        //加载主配置文件


        //print(fileMain);
    }

    /// <summary>
    /// 存放已经加载的AB包 string -- 包名
    /// </summary>
    public Dictionary<string,ResourceABClass> loadedABClass = new Dictionary<string, ResourceABClass>();

    /// <summary>
    /// 待处理的资源请求_低级
    /// </summary>
    public Queue<ResourceRequest> resourceRequests_low = new Queue<ResourceRequest>();
    /// <summary>
    /// 待处理的资源请求
    /// </summary>
    public Queue<ResourceRequest> resourceRequests_high = new Queue<ResourceRequest>();

    /// <summary>
    /// 根据ab包名获取对应的文件资源——高级
    /// </summary>
    /// <param name="abName">包名</param>
    /// <param name="fileName">文件名</param>
    /// <param name="func">回调函数</param>
    public void GetResource(string abName,string fileName, Action<UnityEngine.Object> func, RequestLevel level = RequestLevel.Low) 
    {
        //
        StartCoroutine(GetRes_Cor(abName, fileName, func, level));
        
    }

    private IEnumerator GetRes_Cor(string abName, string fileName, Action<UnityEngine.Object> func, RequestLevel level = RequestLevel.Low) 
    {
        //目标资源请求
        ResourceRequest resourceRequest = new ResourceRequest();

        //print("70------>" + loadedABClass.ContainsKey(abName));
        //如果当前包名存在loadedABClass ,减少反复的LoadFromFileAsync
        if (loadedABClass.ContainsKey(abName))
        {
            //直接就从以获取包名中获得封装AB包
            //设置等待，当第一次加载成功后再继续
            while(loadedABClass[abName] == null)
            {
                //print("loading    " + abName+"    "+ loadedABClass[abName]);
                yield return new WaitForEndOfFrame();
            }

            //创建一个资源请求
            var myab = loadedABClass[abName];

            resourceRequest.abName = abName;
            resourceRequest.fileName = fileName;
            resourceRequest.func = func;
            resourceRequest.level = level;
            resourceRequest.resourceAB = myab;

        }
        //当前没加载过这个包
        else
        {

            //某个ab包占位，开始加载
            loadedABClass.Add(abName, null);

            //根据传进来的ab包名 加载ab包
            AssetBundleCreateRequest abRequest = AssetBundle.LoadFromFileAsync(fileMain + abName);
            //等待协程返回
            yield return abRequest;
            
            if (abRequest.assetBundle == null)
            {
                //停止协程--没有加载到ab包
                Debug.LogError("加载失败，没有找到对应的ab包");
                yield break;
            }

            ResourceABClass resourceAB= new ResourceABClass(abRequest.assetBundle);



            //该请求的request对象封装
            resourceRequest.abName = abRequest.assetBundle.name;
            resourceRequest.fileName = fileName;
            resourceRequest.func = func;
            resourceRequest.level = level;
            resourceRequest.resourceAB = resourceAB;


            //TODO 加载依赖包

            //等待获得依赖结束 




            //将当前加载成功的ab包，放入dic维护，下次可以直接使用
            //获得完依赖后 ，把坑填上

            loadedABClass[abName] = resourceAB;

            yield return GetIndep(resourceAB.name);

        }

        //将资源加载请求resourceRequest放到资源中心队列中 根据优先等级
        if (level == RequestLevel.High)
        {
            resourceRequests_high.Enqueue(resourceRequest);
        }
        else
        {
            resourceRequests_low.Enqueue(resourceRequest);
        }

        yield break;
    }












    /// <summary>
    /// 加载资源包
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    IEnumerator LoadAB(string abName,Action<ResourceABClass> func)
    {
        //占坑
        loadedABClass.Add(abName, null);

        //根据传进来的ab包名 加载ab包
        AssetBundleCreateRequest abRequest = AssetBundle.LoadFromFileAsync(fileMain + abName);

        yield return abRequest;

        //将获得到的封装AB包返回
        if (abRequest.assetBundle == null)
        {
            //停止协程--没有加载到ab包
            Debug.LogError("加载失败，没有找到对应的ab包");
            yield break;
        }

        ResourceABClass resourceAB = new ResourceABClass(abRequest.assetBundle);

        //回调，将生成的ab包节点传出去
        func(resourceAB);

    }

    /// <summary>
    /// 获得AB包的所有依赖
    /// </summary>
    /// <param name="rootAB">根AB包名字</param>
    /// <returns></returns>
    IEnumerator GetIndep(string abName)
    {



        //判断Dic中是否有rootABname
        if (loadedABClass.ContainsKey(abName))
        {
            //不用操作，已经加载过了
            //
            //while(loadedABClass[abName] == null)
            //{
            //    yield return new WaitForEndOfFrame();
            //}
        }
        else
        {
            //开启协程加载加载资源后放进去
            yield return StartCoroutine(LoadAB(abName, (resAB) => {
                //把加载后的封装ab资源 放到dic中，表示已加载
                loadedABClass[resAB.name] = resAB;
            }));
        }

        string[] indep_strs = manifest.GetDirectDependencies(abName);

        if(indep_strs.Length > 0)
        {
            foreach (var indep in indep_strs)
            {
                yield return StartCoroutine(GetIndep(indep));
            }
        }
        else
        {
            yield break;
        }

        

    }

    private void Update()
    {
        //print("长度----》" + loadedABClass.Count);
        //每帧检查 队列是否剩余请求，为了加大检查速度，使用for 扩大内容

       // print("HIGH------>" + resourceRequests_high.Count);
        //print("LOW------>" + resourceRequests_low.Count);

        //优先快的
        for (int i = 0; i < MAX_CHECK_INFRAME_HIGH; i++)
        {
            //判断是否有 快加载的队列 有数据
            if (resourceRequests_high.Count > 0)
            {
                //弹出队列
                ResourceRequest high_req = resourceRequests_high.Dequeue();

                
                var obj = high_req.resourceAB.GetResource(high_req.fileName);

                //调用回调，将obj传出去
                high_req.func(obj);
                Debug.Log("ResMgr:加载资源成功！");
            }
            else
            {
                break;
            }

        }


        //优先慢的
        for (int i = 0; i < MAX_CHECK_INFRAME_LOW; i++)
        {

            //判断是否有 快加载的队列 有数据
            if (resourceRequests_low.Count > 0)
            {
                //弹出队列
                ResourceRequest high_req = resourceRequests_low.Dequeue();


                var obj = high_req.resourceAB.GetResource(high_req.fileName);

                //调用回调，将obj传出去
                high_req.func(obj);
                Debug.Log("ResMgr:加载资源成功！");
            }
            else
            {
                break;
            }


        }
    }


}



/// <summary>
/// 异步创建ResourceAB包的协程返回类
/// </summary>
class CreateResourceABClassRequest : AsyncOperation
{

    public ResourceABClass resource;

    public CreateResourceABClassRequest(ResourceABClass resource)
    {
        this.resource = resource;
    }
}


/// <summary>
/// 每次资源请求封装
/// </summary>
public class ResourceRequest
{

    public RequestLevel level;
    public string abName;
    public string fileName;
    public Action<UnityEngine.Object> func;
    public ResourceABClass resourceAB;

}

/// <summary>
/// 每个ab包类进行封装，封装了依赖数组
/// </summary>
public class ResourceABClass
{
    public List<ResourceABClass> DepABClass ;
    public AssetBundle ab;
    public string name;

    public ResourceABClass(AssetBundle ab)
    {
        this.ab = ab;
        this.name = ab.name;
        this.DepABClass = new List<ResourceABClass>();
    }

    public UnityEngine.Object GetResource(string resName)
    {
        UnityEngine.Object res = ab.LoadAsset<UnityEngine.Object>(resName);

        if (!res)
        {
            Debug.LogError("加载失败,请检查资源名");
        }

        return res;
    }
}
