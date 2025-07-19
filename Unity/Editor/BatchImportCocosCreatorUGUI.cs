using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using LitJson;
using System.Linq;

public class BatchImportCocosCreatorUGUI : EditorWindow
{
    private string jsonFolderPath = "";
    private string prefabOutputPath = "Assets/ImportedPrefabs_UGUI";
    private Vector2 scrollPosition;
    private List<string> logMessages = new List<string>();
    private bool autoCreateCanvas = true;
    private bool preserveFolderStructure = true;

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

        GUILayout.Space(10);

        // 批量导入按钮
        GUI.enabled = !string.IsNullOrEmpty(jsonFolderPath) && Directory.Exists(jsonFolderPath);
        if (GUILayout.Button("开始批量导入", GUILayout.Height(30)))
        {
            BatchImportJsonFiles();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

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

        // 查找所有JSON文件
        string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*.json", SearchOption.AllDirectories);
        AddLog($"找到 {jsonFiles.Length} 个JSON文件");

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < jsonFiles.Length; i++)
        {
            string jsonFile = jsonFiles[i];
            string relativePath = Path.GetRelativePath(jsonFolderPath, jsonFile);
            
            EditorUtility.DisplayProgressBar("批量导入", $"处理: {relativePath}", (float)i / jsonFiles.Length);

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

    void ImportSingleJsonFileUGUI(string jsonFilePath)
    {
        string jsonContent = File.ReadAllText(jsonFilePath);
        JsonData rootData = JsonMapper.ToObject(jsonContent);
        
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
        
        // 保存为Prefab
        string prefabPath = Path.Combine(outputPath, prefabName + "_UGUI.prefab").Replace('\\', '/');
        PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
        
        // 清理场景中的临时对象
        DestroyImmediate(rootObject);
    }

    void AddLog(string message)
    {
        logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        Repaint();
    }
}
