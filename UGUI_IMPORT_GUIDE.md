# UGUI批量导入常见问题解决方案

## 问题1: 导入界面路径选择

### 问题描述
导入界面只让填JSON路径，不知道是否要填resource_list.json所在的路径

### 解决方案
**选择包含prefab JSON文件的文件夹**，而不是resource_list.json所在的根目录。

例如，如果你的导出结构是：
```
导出根目录/
├── resource_list.json          ← 不要选这个目录
└── ui/
    └── res/
        └── ccsFiles/
            ├── MainNode.json   ← 选择包含这些文件的目录
            └── ActivityNode.json
```

**正确操作**: 选择 `ui/res/ccsFiles/` 这样包含实际prefab JSON文件的目录。

## 问题2: JSON解析错误

### 问题描述
导入失败，提示 "The given key 'name/components/children' was not present in the dictionary"

### 解决方案
新版本已经加强了JSON验证和容错处理：

1. **自动跳过resource_list.json**: 勾选"跳过resource_list.json"选项（默认启用）
2. **JSON结构验证**: 工具会自动验证JSON文件是否为有效的prefab文件
3. **容错处理**: 对缺失的字段提供默认值，避免崩溃

### 常见无效JSON类型
- `resource_list.json`: 资源清单文件，不是prefab
- 损坏的JSON文件
- 非Cocos Creator导出的JSON文件

## 问题3: Unity插件日志不能复制

### 问题描述
Unity编辑器工具的日志窗口不能复制文本，不方便反馈问题

### 解决方案
新增了**导出日志到文件**功能：

1. 在批量导入工具界面点击"导出日志到文件"按钮
2. 选择保存路径，日志会以UTF-8格式保存为.txt文件
3. 可以用记事本或其他文本编辑器打开查看
4. 方便复制粘贴给开发者反馈问题

## 问题4: Prefab出现在Hierarchy中

### 问题描述
导入Unity的Prefab出现在了场景的Hierarchy里，2800+ Prefab会导致场景混乱

### 解决方案
新版本已修复此问题：

1. **直接创建Prefab资源**: 所有prefab直接保存到Assets文件夹，不在场景中创建临时对象
2. **自动清理**: 创建prefab后立即删除场景中的临时GameObject
3. **资源选中**: 导入完成后会自动选中Assets中的prefab资源

### 工作流程
1. 创建临时GameObject用于构建prefab结构
2. 保存为Assets中的.prefab文件
3. 立即删除场景中的临时对象
4. 只保留Assets中的prefab资源

## 使用建议

### 分批导入
对于2800+个prefab的大项目：
1. **分目录导入**: 按功能模块分别选择不同的子目录导入
2. **监控内存**: Unity在处理大量prefab时可能消耗较多内存
3. **定期保存**: 分批导入完成后记得保存项目

### 目录结构建议
```
Assets/
└── ImportedPrefabs_UGUI/
    ├── ui/
    │   └── res/
    │       └── ccsFiles/
    │           ├── MainNode_UGUI.prefab
    │           └── ActivityNode_UGUI.prefab
    └── 其他模块/
```

### 导入选项
- **自动创建Canvas**: 如果prefab需要独立显示，建议勾选
- **保持文件夹结构**: 建议勾选，保持与Cocos Creator一致的目录结构
- **跳过resource_list.json**: 建议勾选，避免处理非prefab文件

## 故障排除

### 如果导入仍然失败
1. 检查JSON文件格式是否正确
2. 确认选择的是包含prefab文件的正确目录
3. 使用"导出日志到文件"功能保存详细错误信息
4. 可以先尝试导入单个prefab测试

### 性能优化
- 关闭不必要的Unity编辑器窗口
- 在导入过程中避免其他耗资源操作
- 考虑分批导入而不是一次性导入所有文件
