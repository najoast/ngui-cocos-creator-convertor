using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class ImportCocosCreatorPrefabUGUI : Editor
{
    [MenuItem("Assets/Import From Cocos Creator Json (UGUI)")]
    static void ImportCocosCreatorJsonFileUGUI()
    {
        string filePath = EditorUtility.OpenFilePanel("选择Cocos Creator导出的JSON文件", "", "json");
        if (!string.IsNullOrEmpty(filePath))
        {
            ImportJsonToUGUI(filePath);
        }
    }

    static void ImportJsonToUGUI(string filePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            JObject rootData = JObject.Parse(jsonContent);
            
            // 创建根节点GameObject
            string prefabName = Path.GetFileNameWithoutExtension(filePath);
            GameObject rootObject = new GameObject(prefabName);
            
            // 添加Canvas组件（如果需要）
            Canvas canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootObject.AddComponent<CanvasScaler>();
            rootObject.AddComponent<GraphicRaycaster>();
            
            // 转换节点
            ConvertNodeFromJsonUGUI(rootData, rootObject, null);
            
            // 保存为Prefab
            string prefabPath = "Assets/" + prefabName + "_UGUI.prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
            
            Debug.Log($"成功导入UGUI Prefab: {prefabPath}");
            
            // 立即删除场景中的临时对象
            DestroyImmediate(rootObject);
            
            // 选中Assets中的Prefab资源
            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导入失败: {e.Message}");
        }
    }

    public static GameObject ConvertNodeFromJsonUGUI(JObject nodeData, GameObject parentObject, Transform parent)
    {
        if (nodeData == null) return null;

        // 创建节点
        GameObject nodeObject = new GameObject();
        
        // 设置父级
        if (parent != null)
        {
            nodeObject.transform.SetParent(parent, false);
        }
        else if (parentObject != null)
        {
            nodeObject.transform.SetParent(parentObject.transform, false);
        }

        // 添加RectTransform组件
        RectTransform rectTransform = nodeObject.AddComponent<RectTransform>();

        // 设置基本属性
        if (nodeData["name"] != null)
        {
            nodeObject.name = nodeData["name"].ToString();
        }
        else
        {
            nodeObject.name = "Node"; // 默认名称
        }

        if (nodeData["active"] != null)
        {
            nodeObject.SetActive(nodeData["active"].Value<bool>());
        }

        // 设置位置
        if (nodeData["pos"] != null)
        {
            JObject posData = nodeData["pos"] as JObject;
            Vector3 position = new Vector3(
                posData?["x"]?.Value<float>() ?? 0f,
                posData?["y"]?.Value<float>() ?? 0f,
                posData?["z"]?.Value<float>() ?? 0f
            );
            rectTransform.anchoredPosition3D = position;
        }

        // 设置缩放
        if (nodeData["scale"] != null)
        {
            JObject scaleData = nodeData["scale"] as JObject;
            Vector3 scale = new Vector3(
                scaleData?["x"]?.Value<float>() ?? 1f,
                scaleData?["y"]?.Value<float>() ?? 1f,
                scaleData?["z"]?.Value<float>() ?? 1f
            );
            rectTransform.localScale = scale;
        }

        // 设置旋转
        if (nodeData["rotation"] != null)
        {
            float rotation = nodeData["rotation"].Value<float>();
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        // 设置尺寸
        if (nodeData["size"] != null)
        {
            JObject sizeData = nodeData["size"] as JObject;
            Vector2 size = new Vector2(
                sizeData?["width"]?.Value<float>() ?? 100f,
                sizeData?["height"]?.Value<float>() ?? 100f
            );
            rectTransform.sizeDelta = size;
        }

        // 设置锚点
        if (nodeData["anchor"] != null)
        {
            JObject anchorData = nodeData["anchor"] as JObject;
            Vector2 anchor = new Vector2(
                anchorData?["x"]?.Value<float>() ?? 0.5f,
                anchorData?["y"]?.Value<float>() ?? 0.5f
            );
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
        }

        // 转换组件
        if (nodeData["components"] != null)
        {
            JArray componentsData = nodeData["components"] as JArray;
            if (componentsData != null)
            {
                foreach (JObject component in componentsData)
                {
                    ConvertComponentUGUI(component, nodeObject, rectTransform);
                }
            }
        }

        // 转换子节点
        if (nodeData["children"] != null)
        {
            JArray childrenData = nodeData["children"] as JArray;
            if (childrenData != null)
            {
                foreach (JObject child in childrenData)
                {
                    ConvertNodeFromJsonUGUI(child, null, nodeObject.transform);
                }
            }
        }

        return nodeObject;
    }

    static void ConvertComponentUGUI(JObject componentData, GameObject gameObject, RectTransform rectTransform)
    {
        if (componentData["type"] == null) return;

        string componentType = componentData["type"].ToString();
        
        switch (componentType)
        {
            case "UISprite":
                ConvertUISpriteUGUI(componentData, gameObject, rectTransform);
                break;
            case "UILabel":
                ConvertUILabelUGUI(componentData, gameObject, rectTransform);
                break;
            case "UITexture":
                ConvertUITextureUGUI(componentData, gameObject, rectTransform);
                break;
            case "UIButton":
                ConvertUIButtonUGUI(componentData, gameObject, rectTransform);
                break;
            case "UIScrollView":
                ConvertUIScrollViewUGUI(componentData, gameObject, rectTransform);
                break;
        }
    }

    static void ConvertUISpriteUGUI(JObject componentData, GameObject gameObject, RectTransform rectTransform)
    {
        Image image = gameObject.AddComponent<Image>();
        
        // 设置颜色
        if (componentData["color"] != null)
        {
            JObject colorData = componentData["color"] as JObject;
            Color color = new Color(
                colorData?["r"]?.Value<float>() / 255f ?? 1f,
                colorData?["g"]?.Value<float>() / 255f ?? 1f,
                colorData?["b"]?.Value<float>() / 255f ?? 1f,
                colorData?["a"]?.Value<float>() / 255f ?? 1f
            );
            image.color = color;
        }

        // 设置精灵类型
        if (componentData["type"] != null && componentData["type"].ToString() == "Sliced")
        {
            image.type = Image.Type.Sliced;
        }
        else if (componentData["type"] != null && componentData["type"].ToString() == "Tiled")
        {
            image.type = Image.Type.Tiled;
        }

        // 尝试加载精灵资源
        if (componentData["spriteName"] != null)
        {
            string spriteName = componentData["spriteName"].ToString();
            // 这里可以添加精灵加载逻辑
            Debug.Log($"需要加载精灵: {spriteName}");
        }
    }

    static void ConvertUILabelUGUI(JObject componentData, GameObject gameObject, RectTransform rectTransform)
    {
        Text text = gameObject.AddComponent<Text>();
        
        // 设置文本内容
        if (componentData["text"] != null)
        {
            text.text = componentData["text"].ToString();
        }

        // 设置字体大小
        if (componentData["fontSize"] != null)
        {
            text.fontSize = componentData["fontSize"].Value<int>();
        }

        // 设置颜色
        if (componentData["color"] != null)
        {
            JObject colorData = componentData["color"] as JObject;
            Color color = new Color(
                colorData?["r"]?.Value<float>() / 255f ?? 0f,
                colorData?["g"]?.Value<float>() / 255f ?? 0f,
                colorData?["b"]?.Value<float>() / 255f ?? 0f,
                colorData?["a"]?.Value<float>() / 255f ?? 1f
            );
            text.color = color;
        }

        // 设置对齐方式
        if (componentData["alignment"] != null)
        {
            string alignment = componentData["alignment"].ToString();
            switch (alignment.ToLower())
            {
                case "left":
                    text.alignment = TextAnchor.MiddleLeft;
                    break;
                case "center":
                    text.alignment = TextAnchor.MiddleCenter;
                    break;
                case "right":
                    text.alignment = TextAnchor.MiddleRight;
                    break;
            }
        }

        // 设置默认字体
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static void ConvertUITextureUGUI(JObject componentData, GameObject gameObject, RectTransform rectTransform)
    {
        RawImage rawImage = gameObject.AddComponent<RawImage>();
        
        // 设置颜色
        if (componentData["color"] != null)
        {
            JObject colorData = componentData["color"] as JObject;
            Color color = new Color(
                colorData?["r"]?.Value<float>() / 255f ?? 1f,
                colorData?["g"]?.Value<float>() / 255f ?? 1f,
                colorData?["b"]?.Value<float>() / 255f ?? 1f,
                colorData?["a"]?.Value<float>() / 255f ?? 1f
            );
            rawImage.color = color;
        }

        // 尝试加载纹理
        if (componentData["textureName"] != null)
        {
            string textureName = componentData["textureName"].ToString();
            Debug.Log($"需要加载纹理: {textureName}");
        }
    }

    static void ConvertUIButtonUGUI(JObject componentData, GameObject gameObject, RectTransform rectTransform)
    {
        Button button = gameObject.AddComponent<Button>();
        
        // 添加Image组件作为按钮背景
        Image image = gameObject.GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }
        
        button.targetGraphic = image;

        // 设置颜色
        if (componentData["normalColor"] != null)
        {
            JObject colorData = componentData["normalColor"] as JObject;
            Color color = new Color(
                colorData?["r"]?.Value<float>() / 255f ?? 1f,
                colorData?["g"]?.Value<float>() / 255f ?? 1f,
                colorData?["b"]?.Value<float>() / 255f ?? 1f,
                colorData?["a"]?.Value<float>() / 255f ?? 1f
            );
            
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = color;
            button.colors = colorBlock;
        }
    }

    static void ConvertUIScrollViewUGUI(JObject componentData, GameObject gameObject, RectTransform rectTransform)
    {
        ScrollRect scrollRect = gameObject.AddComponent<ScrollRect>();
        
        // 创建Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(gameObject.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1, 1, 1, 0.01f);
        
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // 创建Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 300); // 默认高度

        // 设置ScrollRect属性
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
    }
}
