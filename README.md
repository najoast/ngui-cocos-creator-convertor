# 插件说明
需求将Unity项目移植到CocosCreator(H5). 如果UI全部重拼一遍太费时费力. 故抽出时间写了这个扩展. 用于Unity NGUI制作的UI prefab移植到Cocos Creator.

**🆕 新功能**: 现在支持**反向转换**！可以将Cocos Creator 2.x的prefab转换为Unity UGUI格式，支持Unity 2022+现代项目。

CocosCreator版本: 
> * 1.10.x及2.x使用 **CocosCreator_1.10_2.x** 中的插件
> * 反向转换(Cocos Creator → Unity)使用 **CocosCreator_1.10_2.x/prefab-exporter** 插件

# 功能特性

## 正向转换 (Unity NGUI → Cocos Creator)
将Unity中prefab的节点父子结构, 以及节点上NGUI的UISprite, UILabel, UITexture等控件的有用信息保存至json文件. 在CocosCreator中解析后再创建.

## 反向转换 (Cocos Creator → Unity UGUI) 
将Cocos Creator的prefab导出为JSON格式，然后在Unity中重建为UGUI结构。使用Unity原生UI组件，适合Unity 2022+现代项目。
- **NGUI版本**: 适用于使用NGUI插件的传统项目

目前可移植项:
> * 节点: position, scale, rotation(仅z轴), active, name
> * UIWidget → Image/RectTransform: 锚点信息, 宽高, 颜色
> * UISprite → Image: 图集, 图片, Sliced/Tiled类型支持
> * UITexture → RawImage: 纹理, 颜色
> * UILabel → Text: 字号, 颜色, 对齐方式, 行间距
> * UIButton → Button: 按钮交互, 颜色状态
> * UIScrollView → ScrollRect: 滚动视图(基础支持)
> * 带有BoxCollider的节点会被挂载Button组件
> * 子节点按层级深度排序

**UGUI版本优势:**
- ✅ 使用Unity原生组件，无依赖问题
- ✅ 更好的性能和现代Unity兼容性  
- ✅ 支持Unity 2022+ LTS版本
- ✅ 更简单的维护和调试

可以覆盖大部分需求.

# 使用方法

## 正向转换 (Unity → Cocos Creator)
1. 将 **Unity** 文件夹内文件放至Unity工程内. 在Prefab上右键导出Json文件.
2. 将 **prefab-creator** 文件夹放至CocosCreator工程packages目录下. 在扩展菜单中选择 **Prefab生成工具** 打开扩展窗口. 配置导出路径以及图片文件夹. 图片/字体文件夹内放入需要的资源, 支持图集和散图, **图集文件名/图集内图片文件名/散图文件名/字体文件名要与Unity端一致!!!**. 之后拖入第一步导出的Json文件, 点生成即可. 
3. 首次创建时需要加载文件夹内的所有图片, 根据图片数量可能需要较长时间. 所以建议移除文件夹内的无用图片资源.

## 反向转换 (Cocos Creator → Unity UGUI)

### 单个Prefab导出
1. 将 **CocosCreator_1.10_2.x/prefab-exporter** 文件夹复制到Cocos Creator项目的packages目录下
2. 重启Cocos Creator，在菜单栏选择 "Prefab导出工具"
3. 选择"单个Prefab"模式，选择要导出的Prefab文件和导出路径

### Unity端导入
4. **重要**: 首先安装Newtonsoft.Json包（详见[安装指南](./NEWTONSOFT_JSON_SETUP.md)）
5. 将 **Unity/Editor/ImportCocosCreatorPrefabUGUI.cs** 和 **Unity/Editor/BatchImportCocosCreatorUGUI.cs** 放入Unity项目的Assets/Editor文件夹
6. 在Unity中右键选择 "Import From Cocos Creator Json (UGUI)"，选择导出的JSON文件即可

**新特性:**
- ✅ 使用世界上最好的JSON库 - Newtonsoft.Json
- ✅ 异常安全，不会因为缺少字段而崩溃
- ✅ 使用Unity原生UI组件 (Image, Text, Button等)
- ✅ 无需额外插件，适合Unity 2022+
- ✅ 更好的性能和现代Unity兼容性
- ✅ Prefab只在Assets中创建，不污染场景

### 批量导出 (适合大量Prefab项目)
1. 在Cocos Creator的"Prefab导出工具"中选择"批量导出"模式
2. 选择包含Prefab文件的目录（支持递归子文件夹）
3. 勾选"同时导出资源清单"选项
4. 点击"批量导出"，工具会自动导出所有Prefab并生成资源清单

### Unity端批量导入
1. **重要**: 首先安装Newtonsoft.Json包（详见[安装指南](./NEWTONSOFT_JSON_SETUP.md)）
2. 将Unity/Editor文件夹中的**UGUI脚本**放入Unity项目的Assets/Editor文件夹
3. 使用 "Assets -> Batch Import From Cocos Creator Folder (UGUI)" 批量导入
4. 选择包含JSON文件的文件夹（注意：选择包含prefab JSON文件的文件夹，不是resource_list.json所在的根目录）
5. 工具会自动跳过resource_list.json，只处理有效的prefab文件

**重要提示:**
- 📋 **必须先安装Newtonsoft.Json**，否则会编译错误
- 📁 导入时选择的文件夹应该是包含具体prefab JSON文件的目录
- 📄 工具会自动跳过resource_list.json等非prefab文件
- 🏗️ 所有prefab只在Assets中创建，不会出现在场景的Hierarchy中
- 📋 可以导出日志到文件，方便查看导入详情

### 资源依赖解决方案
- **自动资源清单**: 批量导出时会生成`resource_list.json`，包含所有Prefab引用的资源信息
- **资源导出工具**: Unity编辑器工具可以根据资源清单自动从Cocos Creator项目复制资源文件
- **缺失资源检查**: 批量导入时会自动检测缺失的资源并提供详细列表

详细的使用说明和问题解决方案请参考：[UGUI导入指南](./UGUI_IMPORT_GUIDE.md)

# 效果预览
![](https://github.com/glegoo/ngui-cocos-creator-convertor/blob/master/example.gif?raw=true)

## 如果这帮到你请我喝杯咖啡吧~:coffee:
![](https://github.com/glegoo/ngui-cocos-creator-convertor/blob/master/hmj.png?raw=true&v=2)