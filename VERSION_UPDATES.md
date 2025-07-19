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
