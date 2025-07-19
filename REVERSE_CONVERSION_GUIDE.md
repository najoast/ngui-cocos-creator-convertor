# Cocos Creator to Unity NGUI 反向转换使用指南

本工具现在支持从Cocos Creator 2.x转换到Unity NGUI的反向转换功能，包括**单个Prefab导出**和**批量导出**功能。

## 反向转换流程

### 方案一：单个Prefab导出

#### 步骤1：在Cocos Creator中导出Prefab为JSON

1. 将 `CocosCreator_1.10_2.x/prefab-exporter` 文件夹复制到你的Cocos Creator项目的 `packages` 目录下
2. 重启Cocos Creator编辑器
3. 在菜单栏选择 "Prefab导出工具"
4. 在打开的面板中：
   - 选择"单个Prefab"模式
   - 选择要导出的Prefab文件
   - 选择JSON文件的导出路径
   - 点击导出按钮

#### 步骤2：在Unity中导入JSON文件

1. 将 `Unity/Editor/ImportCocosCreatorPrefab.cs` 脚本放入Unity项目的 `Assets/Editor` 文件夹中
2. 在Unity中右键点击Project窗口
3. 选择 "Import From Cocos Creator Json"
4. 选择在步骤1中导出的JSON文件
5. 脚本会自动创建对应的Unity Prefab

### 方案二：批量导出（推荐用于大量Prefab项目）

#### 步骤1：批量导出Prefab和资源清单

1. 将 `CocosCreator_1.10_2.x/prefab-exporter` 文件夹复制到你的Cocos Creator项目的 `packages` 目录下
2. 重启Cocos Creator编辑器
3. 在菜单栏选择 "Prefab导出工具"
4. 在打开的面板中：
   - 选择"批量导出"模式
   - 选择包含Prefab文件的目录（如assets文件夹）
   - 勾选"包含子文件夹"（递归导出所有子目录中的Prefab）
   - 勾选"同时导出资源清单"（生成资源依赖信息）
   - 点击"批量导出"按钮
5. 工具会自动处理所有Prefab文件，并显示导出进度

#### 步骤2：批量导入Unity并处理资源依赖

1. 将 `Unity/Editor` 文件夹中的所有脚本放入Unity项目的 `Assets/Editor` 文件夹
2. **批量导入Prefab**：
   - 在Unity中右键选择 "Batch Import From Cocos Creator Folder"
   - 选择包含导出JSON文件的文件夹
   - 工具会自动检测缺失资源并显示详细报告
   - 确认后开始批量导入
3. **导入资源文件**：
   - 打开 "Window -> Cocos Creator Resource Exporter"
   - 设置Cocos Creator项目路径
   - 设置Unity目标路径（建议创建CocosAssets文件夹）
   - 选择资源清单文件（resource_list.json）
   - 点击"根据资源清单导出"自动复制需要的资源文件

## 大项目优化建议

### 针对2800+Prefab的处理策略

1. **分批处理**: 建议按功能模块分批导出，避免一次性处理过多文件
2. **资源预处理**: 先使用资源导出工具将所有必要资源导入Unity
3. **增量导出**: 支持重复导出，已存在的文件会被跳过
4. **进度监控**: 批量导出会显示实时进度，可以随时了解处理状态

### 资源依赖解决方案

1. **自动资源清单**: 
   - 批量导出时生成`resource_list.json`
   - 包含所有Prefab引用的纹理、字体等资源信息
   - 支持资源去重和路径记录

2. **智能资源导入**:
   - Unity资源导出工具根据清单自动查找和复制资源
   - 支持按类型分类存放（纹理、字体等）
   - 自动检测重复资源避免重复复制

3. **缺失资源检查**:
   - 批量导入前自动检测缺失资源
   - 提供详细的缺失资源列表
   - 支持继续导入或先处理资源依赖

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
