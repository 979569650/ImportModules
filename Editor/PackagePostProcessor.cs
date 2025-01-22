using UnityEditor;
using UnityEngine;
using System.IO;

public class PackagePostProcessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // 定义需要复制的文件夹路径（相对于包的根目录）
        string[] foldersToCopy = {
            "Packages/com.epoint.importmodules/Export",
            "Packages/com.epoint.importmodules/Resource"
        };

        // 遍历所有导入的资源
        foreach (var assetPath in importedAssets)
        {
            // 检查是否是目标文件夹中的文件
            foreach (var folderPath in foldersToCopy)
            {
                if (assetPath.StartsWith(folderPath))
                {
                    // 计算目标路径（将 Packages 替换为 Assets）
                    string destinationPath = assetPath.Replace("Packages/com.epoint.importmodules/", "Assets/ImportModules");

                    // 确保目标目录存在
                    string destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // 复制文件
                    if (File.Exists(assetPath) && !File.Exists(destinationPath))
                    {
                        FileUtil.CopyFileOrDirectory(assetPath, destinationPath);
                        Debug.Log($"Copied {assetPath} to {destinationPath}");
                    }
                }
            }
        }

        // 刷新AssetDatabase，确保复制的文件在Unity中可见
        AssetDatabase.Refresh();
    }
}