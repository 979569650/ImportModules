#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;

public class DependencyInstallers : EditorWindow
{
    private readonly static string[] packages = { "com.unity.formats.fbx", "com.unity.cloud.gltfast" };

    [MenuItem("Tools/ImportModules/Install Dependencies", false, 0)]
    public static void InstallDependencies()
    {
        EditorCoroutine.Start(InstallPackagesCoroutine());
    }

    private static IEnumerator InstallPackagesCoroutine()
    {
        Debug.Log("检查并安装依赖项");

        foreach (var package in packages)
        {
            if (!PackageIsInstalled(package))
            {
                Debug.Log($"正在安装包: {package}");
                var request = UnityEditor.PackageManager.Client.Add(package);

                while (!request.IsCompleted)
                {
                    yield return null;
                }

                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    Debug.Log($"包安装成功: {package}");
                }
                else
                {
                    Debug.LogError($"包安装失败: {package}, 错误: {request.Error.message}");
                }
            }
            else
            {
                Debug.Log($"包已安装: {package}");
            }
        }
    }

    private static bool PackageIsInstalled(string packageName)
    {
        var request = UnityEditor.PackageManager.Client.List(true);
        while (!request.IsCompleted) { }

        if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
        {
            foreach (var package in request.Result)
            {
                if (package.name == packageName)
                {
                    Debug.Log("已安装：" + package.name);
                    return true;
                }
            }
        }
        return false;
    }
}

public static class EditorCoroutine
{
    public static void Start(IEnumerator routine)
    {
        EditorApplication.CallbackFunction update = null;
        update = () =>
        {
            if (!routine.MoveNext())
            {
                EditorApplication.update -= update;
            }
        };
        EditorApplication.update += update;
    }
}
#endif