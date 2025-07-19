using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Text;

public class BatchImportCocosCreatorUGUI : EditorWindow
{
    private string jsonFolderPath = "";
    private string prefabOutputPath = "Assets/ImportedPrefabs_UGUI";
    private Vector2 scrollPosition;
    private List<string> logMessages = new List<string>();
    private bool autoCreateCanvas = true;
    private bool preserveFolderStructure = true;
    private bool skipResourceList = true;

    [MenuItem("Assets/Batch Import From Cocos Creator Folder (UGUI)")]
    static void BatchImportFromFolder()
    {
        string folderPath = EditorUtility.OpenFolderPanel("选择包含JSON文件的文件夹", "", "");
        if (!string.IsNullOrEmpty(folderPath))
        {
            BatchImportCocosCreatorUGUI window = GetWindow<BatchImportCocosCreatorUGUI>("批量导入Cocos Creator Prefab (UGUI)");
            window.jsonFolderPath = folderPath;
            window.Show();
        }
    }

    [MenuItem("Window/Cocos Creator Batch Import (UGUI)")]
    static void ShowWindow()
    {
        GetWindow<BatchImportCocosCreatorUGUI>("批量导入Cocos Creator Prefab (UGUI)");
    }

    void OnGUI()
    {
        GUILayout.Label("Cocos Creator to Unity UGUI 批量导入工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // JSON文件夹路径选择
        GUILayout.BeginHorizontal();
        GUILayout.Label("JSON文件夹:", GUILayout.Width(80));
        jsonFolderPath = GUILayout.TextField(jsonFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择包含JSON文件的文件夹", jsonFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                jsonFolderPath = path;
            }
        }
        GUILayout.EndHorizontal();

        // Prefab输出路径
        GUILayout.BeginHorizontal();
        GUILayout.Label("输出路径:", GUILayout.Width(80));
        prefabOutputPath = GUILayout.TextField(prefabOutputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择Prefab输出文件夹", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    prefabOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "输出路径必须在Assets文件夹内", "确定");
                }
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // 选项
        autoCreateCanvas = EditorGUILayout.Toggle("自动创建Canvas", autoCreateCanvas);
        preserveFolderStructure = EditorGUILayout.Toggle("保持文件夹结构", preserveFolderStructure);
        skipResourceList = EditorGUILayout.Toggle("跳过resource_list.json", skipResourceList);

        GUILayout.Space(10);

        // 批量导入按钮
        GUI.enabled = !string.IsNullOrEmpty(jsonFolderPath) && Directory.Exists(jsonFolderPath);
        if (GUILayout.Button("开始批量导入", GUILayout.Height(30)))
        {
            BatchImportJsonFiles();
        }
        GUI.enabled = true;

        GUILayout.Space(5);

        // 导出日志按钮
        if (GUILayout.Button("导出日志到文件"))
        {
            ExportLogToFile();
        }

        GUILayout.Space(5);

        // 清除日志按钮
        if (GUILayout.Button("清除日志"))
        {
            logMessages.Clear();
        }

        GUILayout.Space(5);

        // 日志显示
        GUILayout.Label("导入日志:", EditorStyles.boldLabel);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (string message in logMessages)
        {
            GUILayout.Label(message, EditorStyles.wordWrappedLabel);
        }
        GUILayout.EndScrollView();
    }

    void BatchImportJsonFiles()
    {
        logMessages.Clear();
        AddLog("开始批量导入...");

        if (!Directory.Exists(prefabOutputPath))
        {
            Directory.CreateDirectory(prefabOutputPath);
            AssetDatabase.Refresh();
        }

        // 查找所有JSON文件，过滤掉resource_list.json
        string[] allJsonFiles = Directory.GetFiles(jsonFolderPath, "*.json", SearchOption.AllDirectories);
        List<string> jsonFiles = new List<string>();
        
        foreach (string jsonFile in allJsonFiles)
        {
            string fileName = Path.GetFileName(jsonFile);
            if (skipResourceList && fileName.Equals("resource_list.json", System.StringComparison.OrdinalIgnoreCase))
            {
                AddLog($"跳过: {fileName}");
                continue;
            }
            
            // 验证JSON文件是否是有效的prefab文件
            if (IsValidPrefabJson(jsonFile))
            {
                jsonFiles.Add(jsonFile);
            }
            else
            {
                AddLog($"跳过无效文件: {Path.GetRelativePath(jsonFolderPath, jsonFile)}");
            }
        }
        
        AddLog($"找到 {jsonFiles.Count} 个有效的Prefab JSON文件");

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < jsonFiles.Count; i++)
        {
            string jsonFile = jsonFiles[i];
            string relativePath = Path.GetRelativePath(jsonFolderPath, jsonFile);
            
            EditorUtility.DisplayProgressBar("批量导入", $"处理: {relativePath}", (float)i / jsonFiles.Count);

            try
            {
                ImportSingleJsonFileUGUI(jsonFile);
                successCount++;
                AddLog($"✓ 成功: {relativePath}");
            }
            catch (System.Exception e)
            {
                failCount++;
                AddLog($"✗ 失败: {relativePath} - {e.Message}");
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddLog($"批量导入完成！成功: {successCount}, 失败: {failCount}");
    }
    
    bool IsValidPrefabJson(string jsonFilePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            JObject rootData = JObject.Parse(jsonContent);
            
            // 检查是否包含基本的节点结构
            return rootData != null && (rootData["name"] != null || rootData["children"] != null || rootData["components"] != null);
        }
        catch
        {
            return false;
        }
    }

    void ImportSingleJsonFileUGUI(string jsonFilePath)
    {
        string jsonContent = File.ReadAllText(jsonFilePath);
        JObject rootData = JObject.Parse(jsonContent);
        
        // 确定输出路径
        string relativePath = Path.GetRelativePath(jsonFolderPath, jsonFilePath);
        string outputPath = prefabOutputPath;
        
        if (preserveFolderStructure)
        {
            string directory = Path.GetDirectoryName(relativePath);
            if (!string.IsNullOrEmpty(directory))
            {
                outputPath = Path.Combine(prefabOutputPath, directory);
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
            }
        }

        // 创建根节点GameObject
        string prefabName = Path.GetFileNameWithoutExtension(jsonFilePath);
        GameObject rootObject = new GameObject(prefabName);
        
        // 可选择添加Canvas组件
        if (autoCreateCanvas)
        {
            Canvas canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootObject.AddComponent<CanvasScaler>();
            rootObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // 至少添加RectTransform
            rootObject.AddComponent<RectTransform>();
        }
        
        // 转换节点
        ImportCocosCreatorPrefabUGUI.ConvertNodeFromJsonUGUI(rootData, rootObject, null);
        
        // 保存为Prefab - 直接保存到资源，不在场景中创建
        string prefabPath = Path.Combine(outputPath, prefabName + "_UGUI.prefab").Replace('\\', '/');
        
        // 创建prefab资源
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
        
        // 立即删除场景中的临时对象
        DestroyImmediate(rootObject);
    }
    
    void ExportLogToFile()
    {
        if (logMessages.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有日志可导出", "确定");
            return;
        }

        string logPath = EditorUtility.SaveFilePanel("保存日志", "", "import_log.txt", "txt");
        if (!string.IsNullOrEmpty(logPath))
        {
            StringBuilder sb = new StringBuilder();
            foreach (string message in logMessages)
            {
                sb.AppendLine(message);
            }
            File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            EditorUtility.DisplayDialog("成功", $"日志已保存到:\n{logPath}", "确定");
        }
    }

    void AddLog(string message)
    {
        logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        Repaint();
    }
}
