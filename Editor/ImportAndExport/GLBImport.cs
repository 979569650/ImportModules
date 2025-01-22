using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class GLBImporter : EditorWindow
{
    private System.Type[] requiredComponents = new System.Type[] {
        typeof(Animator)
    };

    #region 定义面板参数
    private string url = "";

    public static string moduleName = "";

    public static string path = "";

    private bool RequestFBX = false;

    private bool Prefab = true;

    private int AvatarTab = 0;
    private readonly string[] AvatarTabNames = { "Male", "Female", "Custom" };

    private Avatar CustomAvatar;
    public static GameObject glbModel = null;
    #endregion


    [MenuItem("Tools/ImportModules/GLB Importer", false, 1)]
    public static void ShowWindow()
    {
        GetWindow<GLBImporter>("GLB Importer");
    }

    #region 绘制面板
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("URL", GUILayout.Width(80)); // 调整标签的宽度
        url = EditorGUILayout.TextField(url, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        TextTip(url, "模型资源url地址");

        GUILayout.BeginHorizontal();
        GUILayout.Label("名称", GUILayout.Width(80));
        moduleName = EditorGUILayout.TextField(moduleName, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        TextTip(moduleName, "默认从url自动生成");


        GetPath(path, "保存路径", "选择保存路径", ref path);
        TextTip(path, "默认为 Assets/ImportModules/Export/ + 名称");

        GUILayout.BeginHorizontal();
        GUILayout.Label("导出FBX", GUILayout.Width(80));
        RequestFBX = EditorGUILayout.Toggle(RequestFBX);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("生成预制体", GUILayout.Width(80));
        Prefab = EditorGUILayout.Toggle(Prefab);
        GUILayout.EndHorizontal();
        if (Prefab)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("选择骨骼模型", GUILayout.Width(80));
            AvatarTab = EditorGUILayout.Popup(AvatarTab, AvatarTabNames, GUILayout.Width(70));
            GUILayout.EndHorizontal();
            if (AvatarTab == 2)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Avatar", GUILayout.Width(80));
                CustomAvatar = (Avatar)EditorGUILayout.ObjectField(CustomAvatar, typeof(Avatar), false, GUILayout.Width(140));
                GUILayout.EndHorizontal();
                if (CustomAvatar == null)
                {
                    EditorGUILayout.HelpBox("请添加一个Avatar", MessageType.Info);
                    return;
                }
            }
        }

        EditorGUILayout.Space(20);
        if (GUILayout.Button("Download and Import"))
        {
            if (Application.isEditor)
            {
                if (url == "")
                {
                    Debug.LogError("url为空");
                    return;
                }
                // 获取通用ID（仅提取一次）
                string extractedId = ExtractIdFromUrl(url);
                // 设置模块名称
                moduleName = GetValidName(moduleName, extractedId);

                const string DefaultPathRoot = "Assets/ImportModules/Export/";
                string DefaultPath = DefaultPathRoot + moduleName;
                // 设置GLB路径
                path = GetValidPath(path, DefaultPath);
                // 启动
                StartDownloadAndImportGLB();
            }
        }
    }
    #endregion

    #region 开始生成
    private void StartDownloadAndImportGLB()
    {
        // 检查GLB路径是否有效
        if (!IsValidPath(path, "Path路径不存在，为您创建" + path))
        {
            return;
        }
        // 创建UnityWebRequest对象
        UnityWebRequest www = UnityWebRequest.Get(url);
        // 发送请求
        www.SendWebRequest();

        // 等待请求完成
        while (!www.isDone)
        {
            // 你可以在这里添加一个进度条更新或其他逻辑
            Debug.Log("等待...");
        }

        // 检查请求是否成功
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error downloading GLB: {www.error}");
        }
        else
        {
            #region GLB模型下载、导入
            string localPath = Path.Combine(path, moduleName + ".glb");
            File.WriteAllBytes(localPath, www.downloadHandler.data);

            AssetDatabase.Refresh();
            Debug.Log("GLB downloaded and imported: " + localPath);
            #endregion

            #region 导出FBX
            if (RequestFBX)
            {
                string assetPath = "Assets/" + localPath[(localPath.IndexOf("Assets/") + 7)..];
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                // model转换为fbx格式
                if (model != null)
                {
                    // 将模型转换为FBX格式
                    glbModel = model;
                    FbxExporter.ExportSelectionToFBX();
                }
            }
            #endregion

            #region 导出预制体
            if (Prefab)
            {
                string assetPath = "Assets/" + localPath[(localPath.IndexOf("Assets/") + 7)..];
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (model != null)
                {
                    string prefabPath = Path.Combine(path, moduleName + ".prefab");
                    Debug.Log(prefabPath);
                    // 创建临时实例
                    GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    try
                    {
                        // 添加组件逻辑
                        AddComponentsToInstance(instance);
                        // 保存为预制体
                        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                        Debug.Log($"预制体已创建：{prefabPath}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"创建预制体失败：{e.Message}");
                    }
                    finally
                    {
                        // 清理临时对象
                        if (instance != null)
                        {
                            DestroyImmediate(instance);
                        }
                    }
                    // 刷新资源数据库
                    AssetDatabase.Refresh();
                }
            }
            #endregion
        }
    }
    #endregion

    /// <summary>
    /// 辅助方法：获取有效名称
    /// </summary>
    private string GetValidName(string currentName, string defaultName)
    {
        return string.IsNullOrEmpty(currentName) ? defaultName : currentName;
    }

    /// <summary>
    /// 辅助方法：获取有效路径
    /// </summary>
    private string GetValidPath(string currentPath, string defaultPath)
    {
        return string.IsNullOrEmpty(currentPath) ? defaultPath : currentPath;
    }

    /// <summary>
    /// 路径判断
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>bool</returns>
    private bool IsValidPath(string path, string errorMessage)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning(errorMessage);
            try
            {
                Directory.CreateDirectory(path);
                Debug.Log("创建" + path + "成功");
                return true;
            }
            catch (System.Exception)
            {
                Debug.LogError("创建" + path + "失败");
                return false;
            }
        }
        return true;
    }


    /// <summary>
    /// 根据url取名函数
    /// </summary>
    /// <param name="url">url</param>/// 
    public static string ExtractIdFromUrl(string url)
    {
        // 确保url不为空
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // 找到'/'的最后一个索引，然后找到'.glb'的索引
        int lastSlashIndex = url.LastIndexOf('/');
        int glbIndex = url.LastIndexOf(".glb");

        // 检查索引有效性
        if (lastSlashIndex == -1 || glbIndex == -1 || lastSlashIndex >= glbIndex)
        {
            return string.Empty; // 返回空字符串以表示无效格式
        }

        // 提取'/‘与’.glb’之间的部分
        return url.Substring(lastSlashIndex + 1, glbIndex - lastSlashIndex - 1);
    }

    /// <summary>
    /// 生成提示文字
    /// </summary>
    /// <param name="option">参数</param>
    /// <param name="tip">提示信息</param>
    public void TextTip(string option, string tip)
    {
        // 显示提示文字（当内容为空且未获得焦点时）
        if (string.IsNullOrEmpty(option) && GUI.GetNameOfFocusedControl() != "moduleName")
        {
            Rect textFieldRect = GUILayoutUtility.GetLastRect();
            Rect inputRect = new(
                textFieldRect.x + 88, // 微调水平位置
                textFieldRect.y,                               // 微调垂直位置
                textFieldRect.width,
                textFieldRect.height
            );

            GUIStyle placeholderStyle = new GUIStyle(EditorStyles.label);
            placeholderStyle.normal.textColor = Color.gray;
            placeholderStyle.fontStyle = FontStyle.Italic;
            GUI.Label(inputRect, tip, placeholderStyle);
        }
    }

    /// <summary>
    /// 生成路径组件
    /// </summary>
    /// <param name="path">当前路径</param>
    /// <param name="label">显示的标签</param>
    /// <param name="folderPanelLabel">文件夹选择面板的标签</param>
    /// <param name="pathField">路径字段的引用</param>
    private void GetPath(string path, string label, string folderPanelLabel, ref string pathField)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        pathField = EditorGUILayout.TextField(path, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel(folderPanelLabel, Application.dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    pathField = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    pathField = pathField.Replace('\\', '/');
                    if (pathField.EndsWith("/"))
                        pathField = pathField.TrimEnd('/');
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "必须选择Assets文件夹内的路径", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 给预制体添加组件
    /// </summary>
    /// <param name="instance">预制体</param>
    private void AddComponentsToInstance(GameObject instance)
    {
        foreach (var componentType in requiredComponents)
        {
            // 仅在不存在组件时添加
            if (instance.GetComponent(componentType) == null)
            {
                var animator = instance.AddComponent<Animator>();

                // 基础动画器配置（按需修改）
                animator.applyRootMotion = false;    // 默认不应用根运动
                animator.updateMode = AnimatorUpdateMode.Normal;
                if (AvatarTab != 2)
                {
                    Debug.Log("Assets/ImportModules/Resource/Avatar/" + AvatarTabNames[AvatarTab] + ".asset");
                    animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ImportModules/Resource/Avatar/" + AvatarTabNames[AvatarTab] + ".asset");
                }
                else
                {
                    animator.avatar = CustomAvatar;
                }
            }
        }
    }
}
