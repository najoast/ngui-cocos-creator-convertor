# UGUI vs NGUI 选择指南

## Unity 2022+ 项目建议

对于Unity 2022及以上版本的项目，强烈建议使用**UGUI版本**的转换工具，原因如下：

## UGUI版本优势

### ✅ 无依赖问题
- 使用Unity原生UI组件 (Image, Text, Button, ScrollRect等)
- 不需要安装任何第三方插件
- 避免了NGUI插件的兼容性问题

### ✅ 现代Unity兼容性
- 完美支持Unity 2022+ LTS版本
- 符合Unity官方UI发展方向
- 长期支持和维护保障

### ✅ 更好的性能
- Unity原生UI系统经过持续优化
- 更好的批处理和渲染性能
- 支持Unity的UI优化特性

### ✅ 更简单的维护
- 无需担心第三方插件更新问题
- Unity官方文档和社区支持更完善
- 更容易调试和修改

## NGUI版本适用场景

### 传统项目
- 已经在使用NGUI的老项目
- 需要保持现有UI框架一致性
- 有特定的NGUI定制需求

## 文件对应关系

| 功能 | UGUI版本 | NGUI版本 |
|-----|---------|---------|
| 单个导入 | `ImportCocosCreatorPrefabUGUI.cs` | `ImportCocosCreatorPrefab.cs` |
| 批量导入 | `BatchImportCocosCreatorUGUI.cs` | `BatchImportCocosCreator.cs` |
| 菜单项 | "Import From Cocos Creator Json (UGUI)" | "Import From Cocos Creator Json" |

## 组件转换对应关系

| Cocos Creator | UGUI | NGUI |
|---------------|------|------|
| cc.Sprite | Image | UISprite |
| cc.Label | Text | UILabel |
| cc.Button | Button | UIButton |
| cc.ScrollView | ScrollRect | UIScrollView |
| cc.Node | RectTransform | UIWidget |

## 推荐使用流程

1. **新项目**: 直接使用UGUI版本
2. **现有NGUI项目**: 可以考虑逐步迁移到UGUI
3. **Unity 2022+**: 强烈推荐UGUI版本

选择UGUI版本，让你的项目更适应Unity的未来发展方向！
