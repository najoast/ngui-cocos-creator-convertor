using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using LitJson;

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
            JsonData rootData = JsonMapper.ToObject(jsonContent);
            
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
            PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
            
            Debug.Log($"成功导入UGUI Prefab: {prefabPath}");
            
            // 选中创建的对象
            Selection.activeGameObject = rootObject;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导入失败: {e.Message}");
        }
    }

    public static GameObject ConvertNodeFromJsonUGUI(JsonData nodeData, GameObject parentObject, Transform parent)
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
        if (nodeData.ContainsKey("name"))
        {
            nodeObject.name = nodeData["name"].ToString();
        }

        if (nodeData.ContainsKey("active"))
        {
            nodeObject.SetActive((bool)nodeData["active"]);
        }

        // 设置位置
        if (nodeData.ContainsKey("pos"))
        {
            JsonData posData = nodeData["pos"];
            Vector3 position = new Vector3(
                (float)(double)posData["x"],
                (float)(double)posData["y"],
                (float)(double)posData["z"]
            );
            rectTransform.anchoredPosition3D = position;
        }

        // 设置缩放
        if (nodeData.ContainsKey("scale"))
        {
            JsonData scaleData = nodeData["scale"];
            Vector3 scale = new Vector3(
                (float)(double)scaleData["x"],
                (float)(double)scaleData["y"],
                (float)(double)scaleData["z"]
            );
            rectTransform.localScale = scale;
        }

        // 设置旋转
        if (nodeData.ContainsKey("rotation"))
        {
            float rotation = (float)(double)nodeData["rotation"];
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        // 设置尺寸
        if (nodeData.ContainsKey("size"))
        {
            JsonData sizeData = nodeData["size"];
            Vector2 size = new Vector2(
                (float)(double)sizeData["width"],
                (float)(double)sizeData["height"]
            );
            rectTransform.sizeDelta = size;
        }

        // 设置锚点
        if (nodeData.ContainsKey("anchor"))
        {
            JsonData anchorData = nodeData["anchor"];
            Vector2 anchor = new Vector2(
                (float)(double)anchorData["x"],
                (float)(double)anchorData["y"]
            );
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
        }

        // 转换组件
        if (nodeData.ContainsKey("components"))
        {
            JsonData componentsData = nodeData["components"];
            for (int i = 0; i < componentsData.Count; i++)
            {
                ConvertComponentUGUI(componentsData[i], nodeObject, rectTransform);
            }
        }

        // 转换子节点
        if (nodeData.ContainsKey("children"))
        {
            JsonData childrenData = nodeData["children"];
            for (int i = 0; i < childrenData.Count; i++)
            {
                ConvertNodeFromJsonUGUI(childrenData[i], null, nodeObject.transform);
            }
        }

        return nodeObject;
    }

    static void ConvertComponentUGUI(JsonData componentData, GameObject gameObject, RectTransform rectTransform)
    {
        if (!componentData.ContainsKey("type")) return;

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

    static void ConvertUISpriteUGUI(JsonData componentData, GameObject gameObject, RectTransform rectTransform)
    {
        Image image = gameObject.AddComponent<Image>();
        
        // 设置颜色
        if (componentData.ContainsKey("color"))
        {
            JsonData colorData = componentData["color"];
            Color color = new Color(
                (float)(double)colorData["r"] / 255f,
                (float)(double)colorData["g"] / 255f,
                (float)(double)colorData["b"] / 255f,
                (float)(double)colorData["a"] / 255f
            );
            image.color = color;
        }

        // 设置精灵类型
        if (componentData.ContainsKey("type") && componentData["type"].ToString() == "Sliced")
        {
            image.type = Image.Type.Sliced;
        }
        else if (componentData.ContainsKey("type") && componentData["type"].ToString() == "Tiled")
        {
            image.type = Image.Type.Tiled;
        }

        // 尝试加载精灵资源
        if (componentData.ContainsKey("spriteName"))
        {
            string spriteName = componentData["spriteName"].ToString();
            // 这里可以添加精灵加载逻辑
            Debug.Log($"需要加载精灵: {spriteName}");
        }
    }

    static void ConvertUILabelUGUI(JsonData componentData, GameObject gameObject, RectTransform rectTransform)
    {
        Text text = gameObject.AddComponent<Text>();
        
        // 设置文本内容
        if (componentData.ContainsKey("text"))
        {
            text.text = componentData["text"].ToString();
        }

        // 设置字体大小
        if (componentData.ContainsKey("fontSize"))
        {
            text.fontSize = (int)(double)componentData["fontSize"];
        }

        // 设置颜色
        if (componentData.ContainsKey("color"))
        {
            JsonData colorData = componentData["color"];
            Color color = new Color(
                (float)(double)colorData["r"] / 255f,
                (float)(double)colorData["g"] / 255f,
                (float)(double)colorData["b"] / 255f,
                (float)(double)colorData["a"] / 255f
            );
            text.color = color;
        }

        // 设置对齐方式
        if (componentData.ContainsKey("alignment"))
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

    static void ConvertUITextureUGUI(JsonData componentData, GameObject gameObject, RectTransform rectTransform)
    {
        RawImage rawImage = gameObject.AddComponent<RawImage>();
        
        // 设置颜色
        if (componentData.ContainsKey("color"))
        {
            JsonData colorData = componentData["color"];
            Color color = new Color(
                (float)(double)colorData["r"] / 255f,
                (float)(double)colorData["g"] / 255f,
                (float)(double)colorData["b"] / 255f,
                (float)(double)colorData["a"] / 255f
            );
            rawImage.color = color;
        }

        // 尝试加载纹理
        if (componentData.ContainsKey("textureName"))
        {
            string textureName = componentData["textureName"].ToString();
            Debug.Log($"需要加载纹理: {textureName}");
        }
    }

    static void ConvertUIButtonUGUI(JsonData componentData, GameObject gameObject, RectTransform rectTransform)
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
        if (componentData.ContainsKey("normalColor"))
        {
            JsonData colorData = componentData["normalColor"];
            Color color = new Color(
                (float)(double)colorData["r"] / 255f,
                (float)(double)colorData["g"] / 255f,
                (float)(double)colorData["b"] / 255f,
                (float)(double)colorData["a"] / 255f
            );
            
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = color;
            button.colors = colorBlock;
        }
    }

    static void ConvertUIScrollViewUGUI(JsonData componentData, GameObject gameObject, RectTransform rectTransform)
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
