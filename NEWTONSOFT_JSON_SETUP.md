# 🚀 升级到 Newtonsoft.Json - 世界上最好的JSON库！

## 为什么要换掉LitJson？

LitJson是个**老古董废物库**，问题一大堆：
- ❌ 异常处理机制垃圾，访问不存在的key直接炸
- ❌ 类型转换繁琐，要写一堆 `(float)(double)` 的屎代码  
- ❌ 性能差，内存占用高
- ❌ 社区支持差，文档烂
- ❌ 功能有限，扩展性差

**Newtonsoft.Json (Json.NET)** 才是王道：
- ✅ 世界上最流行的.NET JSON库，GitHub 13k+ stars
- ✅ Unity官方推荐，内置支持
- ✅ 异常安全，空值检查超简单
- ✅ 性能优异，内存友好
- ✅ 功能强大，LINQ to JSON支持
- ✅ 完善的文档和社区支持

## Unity中安装Newtonsoft.Json

### 方法1：Package Manager (推荐)
1. 打开Unity Editor
2. 打开 `Window > Package Manager`
3. 左上角选择 `Unity Registry`
4. 搜索 `com.unity.nuget.newtonsoft-json`
5. 点击 `Install`

### 方法2：手动添加到manifest.json
1. 打开 `Packages/manifest.json`
2. 在 `dependencies` 中添加：
```json
{
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    ...其他包
  }
}
```
3. 保存文件，Unity会自动安装

## 代码对比 - 看看多么优雅！

### LitJson (垃圾) vs Newtonsoft.Json (优雅)

#### 空值检查
```csharp
// LitJson - 垃圾写法，还可能炸
if (nodeData.ContainsKey("name") && nodeData["name"] != null)
{
    string name = nodeData["name"].ToString();
}

// Newtonsoft.Json - 简洁优雅
if (nodeData["name"] != null)
{
    string name = nodeData["name"].ToString();
}
```

#### 类型转换
```csharp
// LitJson - 屎一样的转换
float x = (float)(double)posData["x"];

// Newtonsoft.Json - 直接搞定
float x = posData["x"]?.Value<float>() ?? 0f;
```

#### 数组遍历
```csharp
// LitJson - 原始写法
for (int i = 0; i < childrenData.Count; i++)
{
    ConvertNode(childrenData[i]);
}

// Newtonsoft.Json - 现代写法
foreach (JObject child in childrenData)
{
    ConvertNode(child);
}
```

## 新代码特点

### 🛡️ 异常安全
- 使用 `?.` 操作符进行空值检查
- `Value<T>()` 方法安全类型转换
- `??` 操作符提供默认值

### 🎯 类型精确
- `JObject` 用于JSON对象
- `JArray` 用于JSON数组  
- `JToken` 作为基类
- 不需要复杂的类型转换

### 💡 代码简洁
- LINQ查询支持
- 链式调用
- 现代C#语法

## 安装完成后

安装Newtonsoft.Json后，你就可以使用升级后的导入工具了！
- ✅ 不再有 "The given key was not present" 异常
- ✅ 更快的JSON解析速度  
- ✅ 更少的内存占用
- ✅ 更稳定的批量导入

**告别LitJson这个垃圾库，拥抱现代化的JSON处理！** 🎉
