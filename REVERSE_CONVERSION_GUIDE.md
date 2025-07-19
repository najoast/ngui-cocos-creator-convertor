# Cocos Creator to Unity NGUI 反向转换使用指南

本工具现在支持从Cocos Creator 2.x转换到Unity NGUI的反向转换功能。

## 反向转换流程

### 步骤1：在Cocos Creator中导出Prefab为JSON

1. 将 `CocosCreator_1.10_2.x/prefab-exporter` 文件夹复制到你的Cocos Creator项目的 `packages` 目录下
2. 重启Cocos Creator编辑器
3. 在菜单栏选择 "Prefab导出工具"
4. 在打开的面板中：
   - 选择要导出的Prefab文件
   - 选择JSON文件的导出路径
   - 点击导出按钮

### 步骤2：在Unity中导入JSON文件

1. 将 `Unity/Editor/ImportCocosCreatorPrefab.cs` 脚本放入Unity项目的 `Assets/Editor` 文件夹中
2. 在Unity中右键点击Project窗口
3. 选择 "Import From Cocos Creator Json"
4. 选择在步骤1中导出的JSON文件
5. 脚本会自动创建对应的Unity Prefab

## 支持的组件转换

### Cocos Creator → Unity NGUI 组件映射

| Cocos Creator | Unity NGUI | 说明 |
|---------------|------------|------|
| cc.Sprite | UISprite | 精灵组件，支持Simple、Sliced、Filled、Tiled类型 |
| cc.Label | UILabel | 文本组件，支持字体、大小、颜色、溢出模式等 |
| cc.Button | BoxCollider | 按钮转换为碰撞器 |
| cc.ScrollView | UIScrollView + UIPanel | 滚动视图组件 |
| cc.Layout | UIGrid | 布局组件转换为网格 |
| cc.Widget | UIWidget | 基础UI组件 |

### 支持的属性转换

- **位置和变换**: position, rotation, scale
- **尺寸**: width, height
- **颜色**: color (RGB)
- **锚点**: anchor → pivot
- **深度**: zOrder → depth
- **激活状态**: active

### 特殊处理

1. **精灵边框**: Cocos Creator的九宫格边框会转换为NGUI的border属性
2. **文本描边**: Label的描边效果会转换为NGUI的effectStyle
3. **填充类型**: Filled类型精灵的填充方向会正确转换
4. **字体资源**: 位图字体会尝试在Assets中查找对应资源

## 注意事项

1. **资源依赖**: 转换后需要手动设置图集、纹理和字体资源的引用
2. **脚本组件**: 自定义脚本组件不会被转换，需要手动添加
3. **动画**: 动画数据不会被转换
4. **特效**: 粒子系统等特效组件不支持转换
5. **UI适配**: 可能需要手动调整UI适配相关设置

## 资源查找规则

脚本会尝试在Unity的Assets文件夹中查找对应的资源：
- **图集**: 查找名称匹配的UIAtlas文件
- **纹理**: 查找名称匹配的Texture2D文件  
- **字体**: 查找名称匹配的UIFont文件

建议在转换前确保所需资源已导入Unity项目中。

## 故障排除

1. **JSON解析失败**: 检查JSON文件格式是否正确
2. **资源引用丢失**: 确保对应的图集、纹理、字体文件已存在于Unity项目中
3. **组件创建失败**: 确保项目中已导入NGUI插件
4. **位置偏移**: 由于坐标系差异，可能需要手动调整某些UI元素的位置

## 扩展开发

如需支持更多组件或特殊转换逻辑，可以修改：
- `CocosCreator_1.10_2.x/prefab-exporter/prefab-exporter.js` - 添加新的组件导出逻辑
- `Unity/Editor/ImportCocosCreatorPrefab.cs` - 添加新的组件导入逻辑

### 版本说明

当前反向转换功能基于Cocos Creator 1.10.x/2.x版本开发，支持：
- 现代的组件系统API
- 更稳定的资源加载机制
- 更好的错误处理和用户反馈

旧版本的CocosCreator目录不再维护，建议统一使用CocosCreator_1.10_2.x版本。
