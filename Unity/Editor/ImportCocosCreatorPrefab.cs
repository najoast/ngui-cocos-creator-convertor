using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using LitJson;

public class ImportCocosCreatorPrefab : Editor
{
    [MenuItem("Assets/Import From Cocos Creator Prefab")]
    static void ImportCocosCreatorPrefabFile()
    {
        string filePath = EditorUtility.OpenFilePanel("选择Cocos Creator Prefab文件", "", "prefab");
        if (!string.IsNullOrEmpty(filePath))
        {
            ImportPrefab(filePath);
        }
    }

    [MenuItem("Assets/Import From Cocos Creator Json")]
    static void ImportCocosCreatorJsonFile()
    {
        string filePath = EditorUtility.OpenFilePanel("选择JSON文件", "", "json");
        if (!string.IsNullOrEmpty(filePath))
        {
            ImportJson(filePath);
        }
    }

    static void ImportPrefab(string prefabPath)
    {
        // 读取Cocos Creator的prefab文件
        string jsonContent = File.ReadAllText(prefabPath);
        ImportFromJson(jsonContent, Path.GetFileNameWithoutExtension(prefabPath));
    }

    static void ImportJson(string jsonPath)
    {
        string jsonContent = File.ReadAllText(jsonPath);
        ImportFromJson(jsonContent, Path.GetFileNameWithoutExtension(jsonPath));
    }

    static void ImportFromJson(string jsonContent, string prefabName)
    {
        try
        {
            JsonData jsonData = JsonMapper.ToObject(jsonContent);
            
            // 创建根节点
            GameObject rootGO = new GameObject(prefabName);
            
            // 转换节点
            ConvertNodeFromJson(jsonData, rootGO);
            
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
            
            // 选中创建的预制体
            Selection.activeObject = prefab;
            EditorUtility.FocusProjectWindow();
            
            Debug.Log("成功导入Cocos Creator预制体: " + prefabPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("导入失败: " + e.Message);
        }
    }

    public static void ConvertNodeFromJson(JsonData nodeData, GameObject gameObject)
    {
        if (nodeData == null) return;

        // 设置基本属性
        if (nodeData.Keys.Contains("name"))
            gameObject.name = nodeData["name"].ToString();

        // 设置激活状态
        if (nodeData.Keys.Contains("active"))
            gameObject.SetActive((bool)nodeData["active"]);

        // 设置Transform属性
        Transform transform = gameObject.transform;
        
        if (nodeData.Keys.Contains("pos"))
        {
            JsonData pos = nodeData["pos"];
            Vector3 position = Vector3.zero;
            if (pos.Keys.Contains("x")) 
            {
                if (pos["x"].IsDouble) position.x = (float)(double)pos["x"];
                else if (pos["x"].IsInt) position.x = (float)(int)pos["x"];
            }
            if (pos.Keys.Contains("y")) 
            {
                if (pos["y"].IsDouble) position.y = (float)(double)pos["y"];
                else if (pos["y"].IsInt) position.y = (float)(int)pos["y"];
            }
            if (pos.Keys.Contains("z")) 
            {
                if (pos["z"].IsDouble) position.z = (float)(double)pos["z"];
                else if (pos["z"].IsInt) position.z = (float)(int)pos["z"];
            }
            transform.localPosition = position;
        }

        if (nodeData.Keys.Contains("scale"))
        {
            JsonData scale = nodeData["scale"];
            Vector3 scaleVec = Vector3.one;
            if (scale.Keys.Contains("x")) 
            {
                if (scale["x"].IsDouble) scaleVec.x = (float)(double)scale["x"];
                else if (scale["x"].IsInt) scaleVec.x = (float)(int)scale["x"];
            }
            if (scale.Keys.Contains("y")) 
            {
                if (scale["y"].IsDouble) scaleVec.y = (float)(double)scale["y"];
                else if (scale["y"].IsInt) scaleVec.y = (float)(int)scale["y"];
            }
            if (scale.Keys.Contains("z")) 
            {
                if (scale["z"].IsDouble) scaleVec.z = (float)(double)scale["z"];
                else if (scale["z"].IsInt) scaleVec.z = (float)(int)scale["z"];
            }
            transform.localScale = scaleVec;
        }

        if (nodeData.Keys.Contains("rotation"))
        {
            float rotation = 0f;
            if (nodeData["rotation"].IsDouble) rotation = (float)(double)nodeData["rotation"];
            else if (nodeData["rotation"].IsInt) rotation = (float)(int)nodeData["rotation"];
            transform.localEulerAngles = new Vector3(0, 0, rotation);
        }

        // 处理组件
        if (nodeData.Keys.Contains("components"))
        {
            JsonData components = nodeData["components"];
            if (components.IsArray)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    ConvertComponent(components[i], gameObject);
                }
            }
        }

        // 添加按钮组件
        if (nodeData.Keys.Contains("button") && (bool)nodeData["button"])
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            // 设置碰撞器尺寸
            if (nodeData.Keys.Contains("components"))
            {
                JsonData components = nodeData["components"];
                if (components.IsArray && components.Count > 0)
                {
                    JsonData firstComp = components[0];
                    if (firstComp.Keys.Contains("size"))
                    {
                        JsonData size = firstComp["size"];
                        float width = firstComp.Keys.Contains("width") ? (float)(double)size["width"] : 100;
                        float height = firstComp.Keys.Contains("height") ? (float)(double)size["height"] : 100;
                        collider.size = new Vector3(width, height, 1);
                    }
                }
            }
        }

        // 处理ScrollView
        if (nodeData.Keys.Contains("scrollView"))
        {
            ConvertScrollView(nodeData["scrollView"], gameObject);
        }

        // 处理Grid/Layout
        if (nodeData.Keys.Contains("grid"))
        {
            ConvertGrid(nodeData["grid"], gameObject);
        }

        // 处理子节点
        if (nodeData.Keys.Contains("children"))
        {
            JsonData children = nodeData["children"];
            if (children.IsArray)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    GameObject childGO = new GameObject("Child");
                    childGO.transform.SetParent(transform);
                    ConvertNodeFromJson(children[i], childGO);
                }
            }
        }
    }

    static void ConvertComponent(JsonData componentData, GameObject gameObject)
    {
        if (componentData == null) return;

        string componentType = componentData.Keys.Contains("type") ? componentData["type"].ToString() : "";

        // 获取基本属性
        Vector2 size = Vector2.zero;
        if (componentData.Keys.Contains("size"))
        {
            JsonData sizeData = componentData["size"];
            if (sizeData.Keys.Contains("width")) 
            {
                if (sizeData["width"].IsDouble) size.x = (float)(double)sizeData["width"];
                else if (sizeData["width"].IsInt) size.x = (float)(int)sizeData["width"];
            }
            if (sizeData.Keys.Contains("height")) 
            {
                if (sizeData["height"].IsDouble) size.y = (float)(double)sizeData["height"];
                else if (sizeData["height"].IsInt) size.y = (float)(int)sizeData["height"];
            }
        }

        Color color = Color.white;
        if (componentData.Keys.Contains("color"))
        {
            string colorHex = componentData["color"].ToString();
            ColorUtility.TryParseHtmlString("#" + colorHex, out color);
        }

        int depth = 0;
        if (componentData.Keys.Contains("depth"))
        {
            if (componentData["depth"].IsInt) depth = (int)componentData["depth"];
            else if (componentData["depth"].IsDouble) depth = (int)(double)componentData["depth"];
        }

        UIWidget.Pivot pivot = UIWidget.Pivot.Center;
        if (componentData.Keys.Contains("pivot"))
        {
            string pivotStr = componentData["pivot"].ToString();
            System.Enum.TryParse(pivotStr, out pivot);
        }

        switch (componentType)
        {
            case "UISprite":
                ConvertUISprite(componentData, gameObject, size, color, depth, pivot);
                break;
            case "UITexture":
                ConvertUITexture(componentData, gameObject, size, color, depth, pivot);
                break;
            case "UILabel":
                ConvertUILabel(componentData, gameObject, size, color, depth, pivot);
                break;
            case "UIWidget":
                ConvertUIWidget(componentData, gameObject, size, color, depth, pivot);
                break;
        }
    }

    static void ConvertUISprite(JsonData componentData, GameObject gameObject, Vector2 size, Color color, int depth, UIWidget.Pivot pivot)
    {
        UISprite sprite = gameObject.AddComponent<UISprite>();
        
        sprite.width = (int)size.x;
        sprite.height = (int)size.y;
        sprite.color = color;
        sprite.depth = depth;
        sprite.pivot = pivot;

        // 设置精灵名称和图集
        if (componentData.Keys.Contains("spName"))
            sprite.spriteName = componentData["spName"].ToString();
        
        if (componentData.Keys.Contains("atlas"))
        {
            string atlasName = componentData["atlas"].ToString();
            // 尝试在Assets文件夹中查找图集
            string[] guids = AssetDatabase.FindAssets(atlasName + " t:UIAtlas");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                UIAtlas atlas = AssetDatabase.LoadAssetAtPath<UIAtlas>(assetPath);
                sprite.atlas = atlas;
            }
        }

        // 设置精灵类型
        if (componentData.Keys.Contains("spType"))
        {
            string spType = componentData["spType"].ToString();
            switch (spType)
            {
                case "Simple":
                    sprite.type = UIBasicSprite.Type.Simple;
                    break;
                case "Sliced":
                    sprite.type = UIBasicSprite.Type.Sliced;
                    break;
                case "Filled":
                    sprite.type = UIBasicSprite.Type.Filled;
                    if (componentData.Keys.Contains("fillDir"))
                    {
                        uint fillDir = (uint)componentData["fillDir"];
                        sprite.fillDirection = (UIBasicSprite.FillDirection)fillDir;
                    }
                    break;
                case "Tiled":
                    sprite.type = UIBasicSprite.Type.Tiled;
                    break;
            }
        }

        // 设置边框
        if (componentData.Keys.Contains("border"))
        {
            JsonData border = componentData["border"];
            Vector4 borderVec = Vector4.zero;
            if (border.Keys.Contains("left")) 
            {
                if (border["left"].IsDouble) borderVec.x = (float)(double)border["left"];
                else if (border["left"].IsInt) borderVec.x = (float)(int)border["left"];
            }
            if (border.Keys.Contains("bottom")) 
            {
                if (border["bottom"].IsDouble) borderVec.y = (float)(double)border["bottom"];
                else if (border["bottom"].IsInt) borderVec.y = (float)(int)border["bottom"];
            }
            if (border.Keys.Contains("right")) 
            {
                if (border["right"].IsDouble) borderVec.z = (float)(double)border["right"];
                else if (border["right"].IsInt) borderVec.z = (float)(int)border["right"];
            }
            if (border.Keys.Contains("top")) 
            {
                if (border["top"].IsDouble) borderVec.w = (float)(double)border["top"];
                else if (border["top"].IsInt) borderVec.w = (float)(int)border["top"];
            }
            sprite.border = borderVec;
        }
    }

    static void ConvertUITexture(JsonData componentData, GameObject gameObject, Vector2 size, Color color, int depth, UIWidget.Pivot pivot)
    {
        UITexture texture = gameObject.AddComponent<UITexture>();
        
        texture.width = (int)size.x;
        texture.height = (int)size.y;
        texture.color = color;
        texture.depth = depth;
        texture.pivot = pivot;

        // 设置纹理
        if (componentData.Keys.Contains("spName"))
        {
            string textureName = componentData["spName"].ToString();
            string[] guids = AssetDatabase.FindAssets(textureName + " t:Texture2D");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                texture.mainTexture = tex;
            }
        }

        // 设置类型
        if (componentData.Keys.Contains("spType"))
        {
            string spType = componentData["spType"].ToString();
            switch (spType)
            {
                case "Simple":
                    texture.type = UIBasicSprite.Type.Simple;
                    break;
                case "Sliced":
                    texture.type = UIBasicSprite.Type.Sliced;
                    break;
                case "Filled":
                    texture.type = UIBasicSprite.Type.Filled;
                    break;
                case "Tiled":
                    texture.type = UIBasicSprite.Type.Tiled;
                    break;
            }
        }
    }

    static void ConvertUILabel(JsonData componentData, GameObject gameObject, Vector2 size, Color color, int depth, UIWidget.Pivot pivot)
    {
        UILabel label = gameObject.AddComponent<UILabel>();
        
        label.width = (int)size.x;
        label.height = (int)size.y;
        label.color = color;
        label.depth = depth;
        label.pivot = pivot;

        // 设置文本内容
        if (componentData.Keys.Contains("text"))
            label.text = componentData["text"].ToString();

        // 设置字体大小
        if (componentData.Keys.Contains("fontSize"))
        {
            if (componentData["fontSize"].IsInt) label.fontSize = (int)componentData["fontSize"];
            else if (componentData["fontSize"].IsDouble) label.fontSize = (int)(double)componentData["fontSize"];
        }

        // 设置溢出模式
        if (componentData.Keys.Contains("overflow"))
        {
            string overflow = componentData["overflow"].ToString();
            switch (overflow)
            {
                case "ShrinkContent":
                    label.overflowMethod = UILabel.Overflow.ShrinkContent;
                    break;
                case "ClampContent":
                    label.overflowMethod = UILabel.Overflow.ClampContent;
                    break;
                case "ResizeFreely":
                    label.overflowMethod = UILabel.Overflow.ResizeFreely;
                    break;
                case "ResizeHeight":
                    label.overflowMethod = UILabel.Overflow.ResizeHeight;
                    break;
            }
        }

        // 设置描边
        if (componentData.Keys.Contains("outlineColor"))
        {
            string outlineColorHex = componentData["outlineColor"].ToString();
            Color outlineColor;
            if (ColorUtility.TryParseHtmlString("#" + outlineColorHex, out outlineColor))
            {
                label.effectStyle = UILabel.Effect.Outline;
                label.effectColor = outlineColor;
                if (componentData.Keys.Contains("outlineWidth"))
                {
                    float outlineWidth = 1f;
                    if (componentData["outlineWidth"].IsDouble) outlineWidth = (float)(double)componentData["outlineWidth"];
                    else if (componentData["outlineWidth"].IsInt) outlineWidth = (float)(int)componentData["outlineWidth"];
                    label.effectDistance = new Vector2(outlineWidth, outlineWidth);
                }
            }
        }

        // 设置字符间距
        if (componentData.Keys.Contains("spacingX"))
        {
            if (componentData["spacingX"].IsInt) label.spacingX = (int)componentData["spacingX"];
            else if (componentData["spacingX"].IsDouble) label.spacingX = (int)(double)componentData["spacingX"];
        }
        if (componentData.Keys.Contains("spacingY"))
        {
            if (componentData["spacingY"].IsInt) label.spacingY = (int)componentData["spacingY"];
            else if (componentData["spacingY"].IsDouble) label.spacingY = (int)(double)componentData["spacingY"];
        }

        // 设置位图字体
        if (componentData.Keys.Contains("bitmapFont"))
        {
            string fontName = componentData["bitmapFont"].ToString();
            string[] guids = AssetDatabase.FindAssets(fontName + " t:UIFont");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                UIFont font = AssetDatabase.LoadAssetAtPath<UIFont>(assetPath);
                label.bitmapFont = font;
            }
        }
    }

    static void ConvertUIWidget(JsonData componentData, GameObject gameObject, Vector2 size, Color color, int depth, UIWidget.Pivot pivot)
    {
        UIWidget widget = gameObject.AddComponent<UIWidget>();
        
        widget.width = (int)size.x;
        widget.height = (int)size.y;
        widget.color = color;
        widget.depth = depth;
        widget.pivot = pivot;
    }

    static void ConvertScrollView(JsonData scrollViewData, GameObject gameObject)
    {
        UIScrollView scrollView = gameObject.AddComponent<UIScrollView>();
        UIPanel panel = gameObject.GetComponent<UIPanel>();
        if (panel == null)
            panel = gameObject.AddComponent<UIPanel>();

        // 设置移动类型
        if (scrollViewData.Keys.Contains("movement"))
        {
            int movement = (int)scrollViewData["movement"];
            scrollView.movement = (UIScrollView.Movement)movement;
        }

        // 设置剪裁区域
        if (scrollViewData.Keys.Contains("size"))
        {
            JsonData size = scrollViewData["size"];
            Vector2 clipSize = Vector2.zero;
            if (size.Keys.Contains("x")) clipSize.x = (float)(double)size["x"];
            if (size.Keys.Contains("y")) clipSize.y = (float)(double)size["y"];
            
            Vector2 clipOffset = Vector2.zero;
            if (scrollViewData.Keys.Contains("offset"))
            {
                JsonData offset = scrollViewData["offset"];
                if (offset.Keys.Contains("x")) clipOffset.x = (float)(double)offset["x"];
                if (offset.Keys.Contains("y")) clipOffset.y = (float)(double)offset["y"];
            }
            
            panel.clipOffset = clipOffset;
            panel.baseClipRegion = new Vector4(clipOffset.x, clipOffset.y, clipSize.x, clipSize.y);
            panel.clipping = UIDrawCall.Clipping.SoftClip;
        }
    }

    static void ConvertGrid(JsonData gridData, GameObject gameObject)
    {
        UIGrid grid = gameObject.AddComponent<UIGrid>();

        // 设置排列方式
        if (gridData.Keys.Contains("arrangement"))
        {
            uint arrangement = (uint)gridData["arrangement"];
            grid.arrangement = (UIGrid.Arrangement)arrangement;
        }

        // 默认设置一些参数
        grid.maxPerLine = 0;
        grid.cellWidth = 100f;
        grid.cellHeight = 100f;
        grid.animateSmoothly = false;
    }
}
