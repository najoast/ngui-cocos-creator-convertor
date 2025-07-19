using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using LitJson;

public class CocosCreatorResourceExporter : EditorWindow
{
    private string cocosProjectPath = "";
    private string unityTargetPath = "";
    private string resourceListPath = "";
    private bool includeTextures = true;
    private bool includeFonts = true;
    private bool createFolders = true;
    private Vector2 scrollPosition;

    [MenuItem("Window/Cocos Creator Resource Exporter")]
    static void ShowWindow()
    {
        GetWindow<CocosCreatorResourceExporter>("资源导出工具");
    }

    void OnGUI()
    {
        GUILayout.Label("Cocos Creator 资源导出工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Cocos Creator项目路径
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cocos Creator 项目路径:", GUILayout.Width(150));
        cocosProjectPath = EditorGUILayout.TextField(cocosProjectPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择Cocos Creator项目文件夹", cocosProjectPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                cocosProjectPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Unity目标路径
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Unity 目标路径:", GUILayout.Width(150));
        unityTargetPath = EditorGUILayout.TextField(unityTargetPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择Unity目标文件夹", unityTargetPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 确保路径在Assets文件夹内
                string assetsPath = Application.dataPath;
                if (path.StartsWith(assetsPath))
                {
                    unityTargetPath = "Assets" + path.Substring(assetsPath.Length);
                }
                else
                {
                    unityTargetPath = path;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // 资源清单路径
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("资源清单文件:", GUILayout.Width(150));
        resourceListPath = EditorGUILayout.TextField(resourceListPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择resource_list.json文件", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                resourceListPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 导出选项
        GUILayout.Label("导出选项:", EditorStyles.boldLabel);
        includeTextures = EditorGUILayout.Toggle("包含纹理/图片", includeTextures);
        includeFonts = EditorGUILayout.Toggle("包含字体", includeFonts);
        createFolders = EditorGUILayout.Toggle("创建分类文件夹", createFolders);

        GUILayout.Space(20);

        // 操作按钮
        GUI.enabled = !string.IsNullOrEmpty(cocosProjectPath) && !string.IsNullOrEmpty(unityTargetPath);
        
        if (GUILayout.Button("根据资源清单导出", GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(resourceListPath))
            {
                ExportFromResourceList();
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请选择资源清单文件", "确定");
            }
        }

        if (GUILayout.Button("导出所有资源", GUILayout.Height(30)))
        {
            ExportAllResources();
        }

        GUI.enabled = true;

        GUILayout.Space(10);

        // 说明文本
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        EditorGUILayout.HelpBox(
            "使用说明:\n" +
            "1. 选择Cocos Creator项目根目录\n" +
            "2. 选择Unity中的目标文件夹(建议在Assets下创建CocosAssets文件夹)\n" +
            "3. 如果有资源清单文件，可以根据清单精确导出需要的资源\n" +
            "4. 或选择导出所有资源(包含assets文件夹下的所有纹理和字体)\n" +
            "5. 导出完成后记得刷新Unity项目\n\n" +
            "注意: 某些Cocos Creator特有的资源格式可能需要手动转换",
            MessageType.Info
        );
        EditorGUILayout.EndScrollView();
    }

    void ExportFromResourceList()
    {
        try
        {
            string jsonContent = File.ReadAllText(resourceListPath);
            JsonData resourceData = JsonMapper.ToObject(jsonContent);

            List<ResourceInfo> resources = new List<ResourceInfo>();

            if (resourceData.Keys.Contains("resources"))
            {
                JsonData resourceArray = resourceData["resources"];
                if (resourceArray.IsArray)
                {
                    for (int i = 0; i < resourceArray.Count; i++)
                    {
                        JsonData resource = resourceArray[i];
                        ResourceInfo info = new ResourceInfo();

                        if (resource.Keys.Contains("name")) info.name = resource["name"].ToString();
                        if (resource.Keys.Contains("type")) info.type = resource["type"].ToString();
                        if (resource.Keys.Contains("path")) info.path = resource["path"].ToString();

                        if (!string.IsNullOrEmpty(info.name) && ShouldExportResource(info.type))
                        {
                            resources.Add(info);
                        }
                    }
                }
            }

            if (resources.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有找到需要导出的资源", "确定");
                return;
            }

            ExportResources(resources);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", "读取资源清单失败: " + e.Message, "确定");
        }
    }

    void ExportAllResources()
    {
        List<ResourceInfo> resources = new List<ResourceInfo>();

        string assetsPath = Path.Combine(cocosProjectPath, "assets");
        if (!Directory.Exists(assetsPath))
        {
            EditorUtility.DisplayDialog("错误", "找不到Cocos Creator的assets文件夹", "确定");
            return;
        }

        // 查找所有纹理文件
        if (includeTextures)
        {
            string[] textureExtensions = { "*.png", "*.jpg", "*.jpeg", "*.psd" };
            foreach (string pattern in textureExtensions)
            {
                string[] files = Directory.GetFiles(assetsPath, pattern, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    ResourceInfo info = new ResourceInfo();
                    info.name = Path.GetFileNameWithoutExtension(file);
                    info.type = "Texture2D";
                    info.path = file;
                    resources.Add(info);
                }
            }
        }

        // 查找所有字体文件
        if (includeFonts)
        {
            string[] fontExtensions = { "*.fnt", "*.ttf", "*.otf" };
            foreach (string pattern in fontExtensions)
            {
                string[] files = Directory.GetFiles(assetsPath, pattern, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    ResourceInfo info = new ResourceInfo();
                    info.name = Path.GetFileNameWithoutExtension(file);
                    info.type = file.EndsWith(".fnt") ? "BitmapFont" : "Font";
                    info.path = file;
                    resources.Add(info);
                }
            }
        }

        if (resources.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有找到可导出的资源", "确定");
            return;
        }

        ExportResources(resources);
    }

    void ExportResources(List<ResourceInfo> resources)
    {
        try
        {
            int successCount = 0;
            int skipCount = 0;

            // 创建目标文件夹
            if (!Directory.Exists(unityTargetPath))
            {
                Directory.CreateDirectory(unityTargetPath);
            }

            string texturesFolder = createFolders ? Path.Combine(unityTargetPath, "Textures") : unityTargetPath;
            string fontsFolder = createFolders ? Path.Combine(unityTargetPath, "Fonts") : unityTargetPath;

            if (createFolders)
            {
                if (!Directory.Exists(texturesFolder)) Directory.CreateDirectory(texturesFolder);
                if (!Directory.Exists(fontsFolder)) Directory.CreateDirectory(fontsFolder);
            }

            for (int i = 0; i < resources.Count; i++)
            {
                ResourceInfo resource = resources[i];
                
                EditorUtility.DisplayProgressBar("导出资源", $"导出 {resource.name}... ({i + 1}/{resources.Count})", (float)(i + 1) / resources.Count);

                try
                {
                    string sourcePath = string.IsNullOrEmpty(resource.path) ? 
                        FindResourceInCocosProject(resource.name, resource.type) : 
                        resource.path;

                    if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                    {
                        Debug.LogWarning($"找不到资源文件: {resource.name}");
                        skipCount++;
                        continue;
                    }

                    string targetFolder = IsTextureType(resource.type) ? texturesFolder : fontsFolder;
                    string targetPath = Path.Combine(targetFolder, Path.GetFileName(sourcePath));

                    // 避免重复复制
                    if (File.Exists(targetPath))
                    {
                        skipCount++;
                        continue;
                    }

                    File.Copy(sourcePath, targetPath, true);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"复制资源失败 {resource.name}: {e.Message}");
                    skipCount++;
                }
            }

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog(
                "导出完成",
                $"资源导出完成!\n成功: {successCount}个\n跳过: {skipCount}个\n\n请刷新Unity项目以查看导入的资源",
                "确定"
            );

            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", "导出失败: " + e.Message, "确定");
        }
    }

    string FindResourceInCocosProject(string resourceName, string resourceType)
    {
        string assetsPath = Path.Combine(cocosProjectPath, "assets");
        
        if (IsTextureType(resourceType))
        {
            string[] textureExtensions = { ".png", ".jpg", ".jpeg", ".psd" };
            foreach (string ext in textureExtensions)
            {
                string[] files = Directory.GetFiles(assetsPath, resourceName + ext, SearchOption.AllDirectories);
                if (files.Length > 0) return files[0];
            }
        }
        else if (resourceType.Contains("Font"))
        {
            string[] fontExtensions = { ".fnt", ".ttf", ".otf" };
            foreach (string ext in fontExtensions)
            {
                string[] files = Directory.GetFiles(assetsPath, resourceName + ext, SearchOption.AllDirectories);
                if (files.Length > 0) return files[0];
            }
        }

        return null;
    }

    bool ShouldExportResource(string resourceType)
    {
        if (IsTextureType(resourceType)) return includeTextures;
        if (resourceType.Contains("Font")) return includeFonts;
        return false;
    }

    bool IsTextureType(string resourceType)
    {
        return resourceType == "Texture2D" || resourceType == "SpriteFrame";
    }

    private class ResourceInfo
    {
        public string name;
        public string type;
        public string path;
    }
}
