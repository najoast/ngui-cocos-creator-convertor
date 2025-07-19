using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using LitJson;

public class BatchImportCocosCreatorPrefabs : Editor
{
    [MenuItem("Assets/Batch Import From Cocos Creator Folder")]
    static void BatchImportFromFolder()
    {
        string folderPath = EditorUtility.OpenFolderPanel("选择包含JSON文件的文件夹", "", "");
        if (!string.IsNullOrEmpty(folderPath))
        {
            BatchImportFromFolder(folderPath);
        }
    }

    static void BatchImportFromFolder(string folderPath)
    {
        try
        {
            // 查找所有JSON文件
            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories);
            
            if (jsonFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "在指定文件夹中没有找到JSON文件", "确定");
                return;
            }

            // 排除资源清单文件
            List<string> prefabJsonFiles = new List<string>();
            string resourceListPath = null;

            foreach (string file in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName == "resource_list")
                {
                    resourceListPath = file;
                }
                else
                {
                    prefabJsonFiles.Add(file);
                }
            }

            if (prefabJsonFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有找到有效的Prefab JSON文件", "确定");
                return;
            }

            // 读取资源清单
            HashSet<string> missingResources = new HashSet<string>();
            if (!string.IsNullOrEmpty(resourceListPath))
            {
                missingResources = CheckMissingResources(resourceListPath);
            }

            // 确认对话框
            int result = EditorUtility.DisplayDialogComplex(
                "批量导入确认",
                $"找到 {prefabJsonFiles.Count} 个Prefab文件\n" +
                (missingResources.Count > 0 ? $"缺少 {missingResources.Count} 个资源引用\n" : "") +
                "是否继续导入？",
                "导入全部",
                "取消",
                "查看缺失资源"
            );

            if (result == 1) // 取消
            {
                return;
            }
            else if (result == 2) // 查看缺失资源
            {
                ShowMissingResourcesWindow(missingResources);
                return;
            }

            // 开始批量导入
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < prefabJsonFiles.Count; i++)
            {
                string jsonFile = prefabJsonFiles[i];
                string fileName = Path.GetFileNameWithoutExtension(jsonFile);
                
                // 显示进度
                EditorUtility.DisplayProgressBar("批量导入", $"导入 {fileName}... ({i + 1}/{prefabJsonFiles.Count})", (float)(i + 1) / prefabJsonFiles.Count);

                try
                {
                    string jsonContent = File.ReadAllText(jsonFile);
                    ImportFromJson(jsonContent, fileName);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"导入失败 {fileName}: {e.Message}");
                    failCount++;
                }
            }

            EditorUtility.ClearProgressBar();

            // 显示结果
            EditorUtility.DisplayDialog(
                "批量导入完成",
                $"导入完成！\n成功: {successCount}个\n失败: {failCount}个" +
                (missingResources.Count > 0 ? $"\n\n注意: 有 {missingResources.Count} 个资源引用缺失，请手动设置" : ""),
                "确定"
            );

            // 刷新项目
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", "批量导入失败: " + e.Message, "确定");
        }
    }

    static HashSet<string> CheckMissingResources(string resourceListPath)
    {
        HashSet<string> missingResources = new HashSet<string>();

        try
        {
            string jsonContent = File.ReadAllText(resourceListPath);
            JsonData resourceData = JsonMapper.ToObject(jsonContent);

            if (resourceData.Keys.Contains("resources"))
            {
                JsonData resources = resourceData["resources"];
                if (resources.IsArray)
                {
                    for (int i = 0; i < resources.Count; i++)
                    {
                        JsonData resource = resources[i];
                        if (resource.Keys.Contains("name") && resource.Keys.Contains("type"))
                        {
                            string resName = resource["name"].ToString();
                            string resType = resource["type"].ToString();

                            if (!string.IsNullOrEmpty(resName))
                            {
                                // 检查资源是否存在
                                if (!CheckResourceExists(resName, resType))
                                {
                                    missingResources.Add($"{resName} ({resType})");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("读取资源清单失败: " + e.Message);
        }

        return missingResources;
    }

    static bool CheckResourceExists(string resourceName, string resourceType)
    {
        switch (resourceType)
        {
            case "SpriteFrame":
            case "Texture2D":
                string[] textureGuids = AssetDatabase.FindAssets(resourceName + " t:Texture2D");
                return textureGuids.Length > 0;

            case "BitmapFont":
            case "Font":
                string[] fontGuids = AssetDatabase.FindAssets(resourceName + " t:Font");
                if (fontGuids.Length == 0)
                {
                    // 也检查UIFont
                    string[] uiFontGuids = AssetDatabase.FindAssets(resourceName + " t:UIFont");
                    return uiFontGuids.Length > 0;
                }
                return true;

            default:
                return false;
        }
    }

    static void ShowMissingResourcesWindow(HashSet<string> missingResources)
    {
        if (missingResources.Count == 0)
        {
            EditorUtility.DisplayDialog("资源检查", "所有资源都已存在", "确定");
            return;
        }

        string message = "以下资源在Unity项目中找不到，导入后需要手动设置:\n\n";
        int count = 0;
        foreach (string resource in missingResources)
        {
            message += "• " + resource + "\n";
            count++;
            if (count >= 20) // 最多显示20个
            {
                message += $"... 还有 {missingResources.Count - count} 个资源\n";
                break;
            }
        }

        message += "\n建议先导入相关资源文件到Unity项目中。";

        EditorUtility.DisplayDialog("缺失资源列表", message, "确定");
    }

    static void ImportFromJson(string jsonContent, string prefabName)
    {
        JsonData jsonData = JsonMapper.ToObject(jsonContent);
        
        // 创建根节点
        GameObject rootGO = new GameObject(prefabName);
        
        // 转换节点
        ImportCocosCreatorPrefab.ConvertNodeFromJson(jsonData, rootGO);
        
        // 创建预制体
        string prefabPath = "Assets/" + prefabName + ".prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        
        // 如果使用Unity 2018.3或更高版本
        #if UNITY_2018_3_OR_NEWER
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
        #else
        GameObject prefab = PrefabUtility.CreatePrefab(prefabPath, rootGO);
        #endif
        
        // 删除临时对象
        DestroyImmediate(rootGO);
    }
}
