# Windows路径处理问题修复

## 🔍 问题分析

### 原始错误
```
导出失败: 处理Prefab时出错: EINVAL: invalid argument, mkdir 'D:\dev\chaos\woe_ui_cocos_export\D:'
```

### 问题原因
1. **绝对路径处理错误**: Windows绝对路径 `D:\dev\chaos\woe_ui\assets\ui\res\ccsFiles\...` 被直接当作相对路径使用
2. **路径分隔符混乱**: Windows的反斜杠 `\` 和Unix的正斜杠 `/` 混用
3. **assets目录定位失败**: 无法正确提取assets之后的相对路径

## 🔧 修复方案

### 1. 智能路径提取
```javascript
// 修复前（错误的路径处理）
let relativePath = originalPath.replace('assets/', '').replace('.prefab', '.json');

// 修复后（智能路径提取）
if (originalPath.includes('assets\\') || originalPath.includes('assets/')) {
    let assetsIndex = originalPath.lastIndexOf('assets');
    if (assetsIndex !== -1) {
        relativePath = originalPath.substring(assetsIndex + 7); // 跳过'assets/'
        relativePath = relativePath.replace(/\\/g, '/'); // 统一使用正斜杠
        relativePath = relativePath.replace('.prefab', '.json');
    }
}
```

### 2. 路径规范化
```javascript
getPrefabNameFromPath(path) {
    if (!path) return 'unknown';
    
    // 处理Windows和Unix路径分隔符
    let normalizedPath = path.replace(/\\/g, '/');
    let parts = normalizedPath.split('/');
    let filename = parts[parts.length - 1];
    
    return filename.replace('.prefab', '');
}
```

### 3. 目录创建安全性
```javascript
ensureDirectoryExists(dirPath) {
    try {
        // 验证路径是否有效
        if (dirPath.length < 3 || dirPath.includes(':') && dirPath.indexOf(':') > 1) {
            throw new Error('无效的目录路径: ' + dirPath);
        }
        
        if (!Fs.existsSync(dirPath)) {
            Fs.mkdirSync(dirPath);
        }
    } catch (error) {
        this.show('创建目录失败: ' + dirPath + ', 错误: ' + error.message);
        throw error;
    }
}
```

## 📊 路径处理示例

### 输入路径
```
D:\dev\chaos\woe_ui\assets\ui\res\ccsFiles\activityV2View\ActivityCommonBarNode.prefab
```

### 处理过程
1. **查找assets位置**: `lastIndexOf('assets')` = 25
2. **提取相对路径**: `ui\res\ccsFiles\activityV2View\ActivityCommonBarNode.prefab`
3. **路径规范化**: `ui/res/ccsFiles/activityV2View/ActivityCommonBarNode.prefab`
4. **替换扩展名**: `ui/res/ccsFiles/activityV2View/ActivityCommonBarNode.json`

### 最终输出
```
D:\dev\chaos\woe_ui_cocos_export\ui\res\ccsFiles\activityV2View\ActivityCommonBarNode.json
```

## 🎯 预期修复效果

### 修复前
```
mkdir 'D:\dev\chaos\woe_ui_cocos_export\D:'  ❌ 无效路径
```

### 修复后
```
创建目录: D:\dev\chaos\woe_ui_cocos_export\ui
创建目录: D:\dev\chaos\woe_ui_cocos_export\ui\res
创建目录: D:\dev\chaos\woe_ui_cocos_export\ui\res\ccsFiles
...
处理路径: D:\...\ActivityCommonBarNode.prefab -> ui/res/ccsFiles/activityV2View/ActivityCommonBarNode.json
导出完成: ui/res/ccsFiles/activityV2View/ActivityCommonBarNode.json
```

## 🚀 测试指导

现在重新运行批量导出，应该看到：
1. **路径处理日志**: `处理路径: ... -> ui/res/ccsFiles/...`
2. **目录创建日志**: `创建目录: ...`
3. **成功导出**: `导出完成: ui/res/ccsFiles/...`
4. **无路径错误**: 不再出现 `EINVAL: invalid argument` 错误

如果仍有问题，请提供新的错误日志进行进一步分析！
