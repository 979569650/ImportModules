using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using System.Collections.Generic;
using System.Linq;

public class FbxExporter : MonoBehaviour
{
    // 用于存储所有材质
    private static HashSet<Material> materials = new();
    // [MenuItem("Tools/Export Selection to FBX")]
    public static void ExportSelectionToFBX()
    {
        string path = CorrectStringPath(GLBImporter.path) + CorrectStringName(GLBImporter.moduleName);
        if (GLBImporter.glbModel == null)
        {
            Debug.LogWarning("没有选中的对象!");
            return;
        }
        else
        {
            MaterialLister(GLBImporter.glbModel);
            ModelExporter.ExportObject(path, GLBImporter.glbModel);
            GLBImporter.glbModel = null;

            // 指定模型的路径 path
            // 加载模型
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
            {
                // 加载材质
                if (materials != null)
                {
                    // 遍历模型的Renderer组件
                    Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        // 修改每个Renderer的材质
                        renderer.sharedMaterial = materials.FirstOrDefault(material => material.name == renderer.sharedMaterial.name);// 使用 sharedMaterial
                    }
                    Debug.Log("材质已成功更改。");
                }
                else
                {
                    Debug.LogError("未能加载新材质。");
                }
            }
            else
            {
                Debug.LogError("未能加载模型。");
            }
        }
    }

    private static string CorrectStringPath(string url)
    {
        if (!url.EndsWith("/"))
        {
            url += "/";
        }
        return url;
    }

    private static string CorrectStringName(string name)
    {
        if (!name.EndsWith(".fbx"))
        {
            name += ".fbx";
        }
        return name;
    }


    public static void MaterialLister(GameObject gameObject)
    {
        // 获取所有子对象的Renderer组件
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        // 遍历所有Renderer
        foreach (Renderer r in renderers)
        {
            // 获取共享材质
            Material[] materialsArray = r.sharedMaterials;

            // 将材质加入集合中
            foreach (Material mat in materialsArray)
            {
                if (mat != null)
                {
                    materials.Add(mat);
                }
            }
        }
    }
}