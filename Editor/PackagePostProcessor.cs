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
                    string destinationPath = assetPath.Replace("Packages/com.epoint.importmodules/", "Assets/ImportModules/");

                    // 如果是文件夹，递归复制文件夹中的所有内容
                    if (Directory.Exists(assetPath))
                    {
                        CopyDirectory(assetPath, destinationPath);
                    }
                    // 如果是文件，直接复制
                    else if (File.Exists(assetPath))
                    {
                        // 确保目标目录存在
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        FileUtil.CopyFileOrDirectory(assetPath, destinationPath);
                        Debug.Log($"Copied {assetPath} to {destinationPath}");
                    }
                }
            }
        }

        // 刷新AssetDatabase，确保复制的文件在Unity中可见
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 递归复制文件夹中的所有内容
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        // 确保目标目录存在
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        // 复制所有文件
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, true); // 覆盖已存在的文件
            Debug.Log($"Copied {file} to {destFile}");
        }

        // 递归复制子文件夹
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            string destSubDir = Path.Combine(destinationDir, dirName);
            CopyDirectory(subDir, destSubDir); // 递归调用
        }
    }
}