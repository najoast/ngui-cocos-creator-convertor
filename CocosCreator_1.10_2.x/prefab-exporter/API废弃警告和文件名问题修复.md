# API废弃警告和文件名问题修复

## 🔧 修复内容

### 1. 废弃API警告修复 ✅

#### 问题1: `cc.Asset.url` is deprecated
```javascript
// 修复前（会产生警告）
path: sprite.spriteFrame._texture.url

// 修复后（使用新API）
path: sprite.spriteFrame._texture.nativeUrl || sprite.spriteFrame._texture.url
```

#### 问题2: `cc.Node.rotation` is deprecated
```javascript
// 修复前（会产生警告）
nodeData.rotation = Math.round(node.rotation * 100) / 100;

// 修复后（兼容新旧API）
let rotationValue = 0;
if (typeof node.angle !== 'undefined') {
    rotationValue = -node.angle; // 新API使用负值
} else if (typeof node.rotation !== 'undefined') {
    rotationValue = node.rotation; // 兼容旧API
}
nodeData.rotation = Math.round(rotationValue * 100) / 100;
```

### 2. 文件名覆盖问题修复 ✅

#### 问题原因
- 原来使用 `rootNode.name` 作为文件名，但通常是 "Layer" 或 "Node"
- 导致所有prefab都导出为相同的文件名，互相覆盖

#### 解决方案
```javascript
// 修复前
let fileName = rootNode.name || ('prefab_' + prefabUuid.substring(0, 8));
let filePath = Path.join(exportPath, fileName + '.json');

// 修复后
if (originalPath) {
    // 从原始路径提取文件名，保持目录结构
    fileName = self.getPrefabNameFromPath(originalPath);
    
    // 创建相对路径结构：assets/ui/login.prefab -> ui/login.json
    let relativePath = originalPath.replace('assets/', '').replace('.prefab', '.json');
    filePath = Path.join(exportPath, relativePath);
    
    // 确保目录存在
    let dirPath = Path.dirname(filePath);
    self.ensureDirectoryExists(dirPath);
}
```

### 3. 功能增强

#### 自动创建目录结构
```javascript
ensureDirectoryExists(dirPath) {
    if (!Fs.existsSync(dirPath)) {
        // 递归创建目录
        let parentDir = Path.dirname(dirPath);
        if (parentDir !== dirPath) {
            this.ensureDirectoryExists(parentDir);
        }
        Fs.mkdirSync(dirPath);
    }
}
```

#### 传递原始路径信息
- 所有导出方法现在都接收 `originalPath` 参数
- 确保能够正确提取prefab的真实名称和路径

## 📁 预期结果

### 修复前
```
导出目录/
├── Layer.json     (被覆盖多次)
└── Node.json      (被覆盖多次)
```

### 修复后
```
导出目录/
├── ui/
│   ├── login.json
│   ├── main.json
│   └── dialog/
│       ├── confirm.json
│       └── alert.json
├── game/
│   ├── player.json
│   └── enemy.json
```

## 🎯 使用效果

1. **不再有API废弃警告** - 控制台清爽了
2. **保持原有目录结构** - 便于组织和查找
3. **使用真实文件名** - `assets/ui/login.prefab` → `ui/login.json`
4. **避免文件覆盖** - 每个prefab都有唯一的输出文件

## 🚀 立即测试

现在重新运行批量导出应该看到：
- 控制台没有废弃API警告
- 每个prefab导出到正确的路径和文件名
- 自动创建必要的子目录
- 导出进度显示相对路径，如 "导出完成: ui/login.json"

测试后应该能在导出目录看到完整的目录结构，每个prefab都有对应的JSON文件！
