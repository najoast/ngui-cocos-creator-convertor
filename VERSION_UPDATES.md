# 版本更新说明

## 反向转换功能更新 (Cocos Creator → Unity NGUI)

### 变更内容

1. **移除旧版本支持**: 
   - 删除了 `CocosCreator/prefab-exporter` 目录
   - 不再维护基于Cocos Creator 1.9.x的旧版本

2. **统一版本管理**:
   - 反向转换功能现在基于 `CocosCreator_1.10_2.x/prefab-exporter`
   - 适配Cocos Creator 1.10.x 和 2.x版本
   - 使用更稳定的API和更好的兼容性

3. **功能增强**:
   - 改进了组件检测和转换逻辑
   - 增强了错误处理和用户反馈
   - 优化了资源名称提取算法
   - 支持更多UI组件属性的转换

### 🆕 批量处理功能 v2.0

1. **批量导出支持**:
   - 支持批量导出整个目录中的所有Prefab文件
   - 递归扫描子文件夹选项
   - 实时进度显示，支持大量文件处理
   - 适合2800+Prefab这样的大型项目

2. **资源依赖管理**:
   - 自动收集Prefab引用的所有资源（纹理、字体等）
   - 生成详细的资源清单文件（resource_list.json）
   - 支持资源去重和UUID跟踪
   - 包含完整的资源路径信息

3. **Unity批量导入工具**:
   - 新增`BatchImportCocosCreatorPrefabs.cs`脚本
   - 支持文件夹级别的批量导入
   - 自动检测缺失资源并提供详细报告
   - 导入前预览和确认功能

4. **资源导出助手**:
   - 新增`CocosCreatorResourceExporter.cs`可视化工具
   - 根据资源清单自动导出资源文件
   - 支持按类型分类存放资源
   - 智能重复检测避免重复复制

### 使用方法

#### 正向转换 (Unity NGUI → Cocos Creator)
- 使用 `CocosCreator_1.10_2.x/prefab-creator` 插件（保持不变）

#### 反向转换 (Cocos Creator → Unity NGUI)
- 使用 `CocosCreator_1.10_2.x/prefab-exporter` 插件（新增）
- 配合 `Unity/Editor/ImportCocosCreatorPrefab.cs` 脚本

### 兼容性

- **Cocos Creator**: 1.10.x, 2.0.x, 2.1.x, 2.2.x, 2.3.x, 2.4.x
- **Unity**: 2018.x 及以上版本
- **NGUI**: 3.8.2 及以上版本

### 迁移指南

如果您之前使用过旧版本的导出功能：

1. 删除项目packages目录下的旧版 `prefab-exporter` 文件夹
2. 复制新版 `CocosCreator_1.10_2.x/prefab-exporter` 到packages目录
3. 重启Cocos Creator编辑器
4. 功能使用方式保持不变

### 已知问题和解决方案

1. **组件获取失败**: 
   - 新版本增加了多种兼容性检查
   - 自动处理不同版本API差异

2. **资源引用丢失**:
   - 改进了图集和纹理名称提取逻辑
   - 建议在Unity中手动检查资源引用

3. **特殊组件处理**:
   - 增强了对自定义组件的处理
   - 未知组件会自动转换为UIWidget

详细使用说明请参考：[反向转换使用指南](./REVERSE_CONVERSION_GUIDE.md)
