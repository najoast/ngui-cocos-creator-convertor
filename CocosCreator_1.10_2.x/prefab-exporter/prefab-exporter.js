const Fs = require('fs');
const Path = require('path');

// Cocos Creator组件类型映射
const ComponentTypeMap = {
    'cc.Sprite': 'UISprite',
    'cc.Label': 'UILabel',
    'cc.Button': 'UIButton',
    'cc.ScrollView': 'UIScrollView',
    'cc.Layout': 'UIGrid',
    'cc.Widget': 'UIWidget'
};

// Pivot映射 - 将Cocos Creator的anchor转换为NGUI的pivot
const getPivotFromAnchor = (anchorX, anchorY) => {
    if (anchorY <= 0.1) { // 下
        if (anchorX <= 0.1) return 'BottomLeft';
        if (anchorX >= 0.9) return 'BottomRight';
        return 'Bottom';
    } else if (anchorY >= 0.9) { // 上
        if (anchorX <= 0.1) return 'TopLeft';
        if (anchorX >= 0.9) return 'TopRight';
        return 'Top';
    } else { // 中
        if (anchorX <= 0.1) return 'Left';
        if (anchorX >= 0.9) return 'Right';
        return 'Center';
    }
};

module.exports = {
    'export': function (event, info) {
        this.show('开始导出Prefab: ' + info.prefab);
        
        if (!info.prefab) {
            this.sendError('请选择Prefab文件!');
            return;
        }

        this.exportPrefab(info.prefab, info.exportPath, info.exportResources || false);
    },

    'batchExport': function (event, info) {
        this.show('开始批量导出，目录: ' + info.prefabFolder);
        
        if (!info.prefabFolder) {
            this.sendError('请选择Prefab目录!');
            return;
        }

        this.batchExportPrefabs(info.prefabFolder, info.exportPath, info.includeSubfolders, info.exportResources);
    },

    batchExportPrefabs(prefabFolder, exportPath, includeSubfolders, exportResources) {
        let self = this;
        
        self.show('开始扫描目录: ' + prefabFolder);
        
        // 首先测试API可用性
        self.testAPIAvailability();
        
        // 规范化路径格式，确保以db://开头，移除多余的斜杠
        let normalizedPath = prefabFolder;
        if (!normalizedPath.startsWith('db://')) {
            normalizedPath = 'db://' + normalizedPath;
        }
        
        // 移除路径末尾的多余斜杠
        normalizedPath = normalizedPath.replace(/\/+$/, '');
        
        // 构建搜索模式
        let searchPattern;
        if (includeSubfolders) {
            // 递归搜索子目录
            searchPattern = normalizedPath + '/**/*';
        } else {
            // 只搜索当前目录
            searchPattern = normalizedPath + '/*';
        }
        
        self.show('规范化路径: ' + normalizedPath);
        self.show('搜索模式: ' + searchPattern);
        
        // 使用多种方式查询prefab文件，增强兼容性
        self.queryPrefabFiles(searchPattern, function(results) {
            if (!results || results.length === 0) {
                // 如果第一次查询失败，尝试不同的查询方式
                self.show('第一次查询结果为空，尝试备用查询方式...');
                self.fallbackQueryPrefabs(normalizedPath, includeSubfolders, function(fallbackResults) {
                    if (!fallbackResults || fallbackResults.length === 0) {
                        // 第三种尝试：直接查询所有prefab然后过滤
                        self.show('备用查询也为空，尝试全局查询...');
                        self.globalQueryPrefabs(normalizedPath, includeSubfolders, function(globalResults) {
                            if (!globalResults || globalResults.length === 0) {
                                self.sendError('在指定目录下没有找到Prefab文件\n目录: ' + prefabFolder + '\n规范化路径: ' + normalizedPath + '\n\n可能的原因:\n1. 项目中确实没有.prefab文件\n2. Cocos Creator版本API不兼容\n3. 资源数据库未正确初始化\n\n请检查项目中是否真的有.prefab文件');
                                return;
                            }
                            self.processPrefabResults(globalResults, exportPath, exportResources);
                        });
                        return;
                    }
                    self.processPrefabResults(fallbackResults, exportPath, exportResources);
                });
                return;
            }
            
            self.processPrefabResults(results, exportPath, exportResources);
        });
    },

    testAPIAvailability() {
        let self = this;
        
        self.show('=== API可用性测试 ===');
        self.show('Editor.assetdb 可用: ' + (Editor.assetdb ? '是' : '否'));
        self.show('Editor.assetdb.queryAssets 可用: ' + (Editor.assetdb && Editor.assetdb.queryAssets ? '是' : '否'));
        self.show('Editor.assetdb.queryInfos 可用: ' + (Editor.assetdb && Editor.assetdb.queryInfos ? '是' : '否'));
        self.show('编辑器版本: ' + (Editor.versions ? JSON.stringify(Editor.versions) : '未知'));
        self.show('=== API测试完成 ===');
    },

    queryPrefabFiles(searchPattern, callback) {
        let self = this;
        
        self.show('开始主查询，模式: ' + searchPattern);
        
        // 方法1: 使用资产数据库查询prefab类型
        Editor.assetdb.queryAssets(searchPattern, 'cc.Prefab', function (err, results) {
            if (err) {
                self.show('主查询错误: ' + err.message);
                // 尝试不指定类型的查询
                self.queryWithoutType(searchPattern, callback);
                return;
            }
            
            self.show('主查询(指定cc.Prefab类型)找到 ' + (results ? results.length : 0) + ' 个文件');
            
            if (!results || results.length === 0) {
                // 尝试不指定类型的查询
                self.queryWithoutType(searchPattern, callback);
                return;
            }
            
            callback(results || []);
        });
    },

    queryWithoutType(searchPattern, callback) {
        let self = this;
        
        self.show('尝试不指定类型的查询: ' + searchPattern);
        
        // 方法2: 不指定资产类型，查询所有匹配路径的资产
        Editor.assetdb.queryAssets(searchPattern, null, function (err, results) {
            if (err) {
                self.show('无类型查询错误: ' + err.message);
                callback([]);
                return;
            }
            
            if (!results || results.length === 0) {
                self.show('无类型查询也无结果');
                callback([]);
                return;
            }
            
            self.show('无类型查询找到 ' + results.length + ' 个资产，开始筛选prefab...');
            
            // 筛选出prefab文件
            let prefabResults = [];
            for (let result of results) {
                if (self.isPrefabAsset(result)) {
                    prefabResults.push(result);
                    if (prefabResults.length <= 10) {
                        self.show('筛选到prefab: ' + result.path + ' (type: ' + result.type + ')');
                    }
                }
            }
            
            self.show('从路径查询中筛选出 ' + prefabResults.length + ' 个prefab文件');
            callback(prefabResults);
        });
    },

    isPrefabAsset(asset) {
        if (!asset) return false;
        
        // 多种方式判断是否为prefab
        if (asset.type === 'cc.Prefab') return true;
        if (asset.type === 'prefab') return true;
        if (asset.url && asset.url.endsWith('.prefab')) return true;
        if (asset.path && asset.path.endsWith('.prefab')) return true;
        
        return false;
    },

    fallbackQueryPrefabs(basePath, includeSubfolders, callback) {
        let self = this;
        
        // 方法2: 尝试直接查询所有prefab类型资源
        let queryUrl = includeSubfolders ? 'db://assets/**/*' : 'db://assets/*';
        
        self.show('备用查询URL: ' + queryUrl);
        
        Editor.assetdb.queryAssets(queryUrl, 'cc.Prefab', function (err, results) {
            if (err) {
                self.show('备用查询也失败: ' + err.message);
                callback([]);
                return;
            }
            
            if (!results) {
                callback([]);
                return;
            }
            
            // 过滤出在指定路径下的prefab
            let filteredResults = [];
            let targetPath = basePath.replace('db://', '');
            
            self.show('过滤目标路径: ' + targetPath);
            
            for (let result of results) {
                if (result.path && result.path.startsWith(targetPath)) {
                    filteredResults.push(result);
                    self.show('匹配: ' + result.path);
                }
            }
            
            self.show('备用查询找到 ' + filteredResults.length + ' 个匹配的Prefab文件');
            callback(filteredResults);
        });
    },

    globalQueryPrefabs(basePath, includeSubfolders, callback) {
        let self = this;
        
        // 方法3: 尝试多种查询方式
        self.show('执行全局查询...');
        
        // 首先尝试查询项目assets目录下的资源
        let projectAssetsPattern = 'db://assets/**/*';
        
        Editor.assetdb.queryAssets(projectAssetsPattern, 'cc.Prefab', function (err, projectResults) {
            if (err) {
                self.show('项目资源查询失败: ' + err.message);
                self.tryAlternativeQuery(basePath, includeSubfolders, callback);
                return;
            }
            
            if (!projectResults || projectResults.length === 0) {
                self.show('项目资源查询无结果，尝试其他方式...');
                self.tryAlternativeQuery(basePath, includeSubfolders, callback);
                return;
            }
            
            self.show('项目资源查询找到 ' + projectResults.length + ' 个prefab文件');
            
            // 过滤掉编辑器内置资源，只保留项目资源
            let filteredProjectResults = [];
            for (let result of projectResults) {
                if (result.path && result.path.startsWith('assets/') && 
                    !result.path.includes('default-assets') && 
                    !result.path.includes('internal')) {
                    filteredProjectResults.push(result);
                    if (filteredProjectResults.length <= 10) {
                        self.show('项目prefab: ' + result.path);
                    }
                }
            }
            
            self.show('过滤后项目prefab数量: ' + filteredProjectResults.length);
            
            if (filteredProjectResults.length === 0) {
                self.show('没有找到项目prefab文件，尝试其他查询...');
                self.tryAlternativeQuery(basePath, includeSubfolders, callback);
                return;
            }
            
            // 按路径进一步过滤
            self.filterPrefabsByPath(filteredProjectResults, basePath, includeSubfolders, callback);
        });
    },

    tryAlternativeQuery(basePath, includeSubfolders, callback) {
        let self = this;
        
        // 尝试查询所有资产，然后过滤
        self.show('尝试查询所有资产...');
        
        Editor.assetdb.queryAssets('**/*', null, function (err, allResults) {
            if (err) {
                self.show('全局查询所有资产失败: ' + err.message);
                callback([]);
                return;
            }
            
            if (!allResults || allResults.length === 0) {
                self.show('全局查询所有资产无结果');
                callback([]);
                return;
            }
            
            self.show('全局查询到 ' + allResults.length + ' 个资产文件，开始详细分析...');
            
            // 统计资产类型
            let typeStats = {};
            let prefabResults = [];
            let debugCount = 0;
            
            for (let result of allResults) {
                // 统计类型
                let resultType = result.type || 'unknown';
                typeStats[resultType] = (typeStats[resultType] || 0) + 1;
                
                // 输出前20个资产的详细信息用于调试
                if (debugCount < 20 && result.path && result.path.startsWith('assets/')) {
                    self.show(`调试资产 ${debugCount + 1}: path="${result.path}", type="${result.type}", url="${result.url}"`);
                    debugCount++;
                }
                
                // 多种方式识别prefab
                let isPrefab = false;
                
                if (result.type === 'cc.Prefab') {
                    isPrefab = true;
                } else if (result.type === 'prefab') {
                    isPrefab = true;
                } else if (result.url && result.url.endsWith('.prefab')) {
                    isPrefab = true;
                } else if (result.path && result.path.endsWith('.prefab')) {
                    isPrefab = true;
                }
                
                // 只包含项目内的prefab，排除编辑器内置资源
                if (isPrefab && result.path && result.path.startsWith('assets/')) {
                    let path = result.path.toLowerCase();
                    let isBuiltIn = path.includes('default-assets') || 
                                   path.includes('internal') || 
                                   path.includes('creator/') ||
                                   path.includes('programdata');
                    
                    if (!isBuiltIn) {
                        prefabResults.push(result);
                        if (prefabResults.length <= 10) {
                            self.show('发现项目prefab: ' + result.path + ' (type: ' + result.type + ')');
                        }
                    }
                }
            }
            
            // 显示类型统计
            self.show('=== 资产类型统计 ===');
            let sortedTypes = Object.keys(typeStats).sort((a, b) => typeStats[b] - typeStats[a]);
            for (let i = 0; i < Math.min(10, sortedTypes.length); i++) {
                let type = sortedTypes[i];
                self.show(`${type}: ${typeStats[type]}个`);
            }
            self.show('=== 统计完成 ===');
            
            self.show('筛选出 ' + prefabResults.length + ' 个项目prefab文件');
            
            if (prefabResults.length === 0) {
                // 尝试更宽松的条件
                self.show('尝试更宽松的prefab识别条件...');
                for (let result of allResults) {
                    if (result.path && result.path.includes('.prefab') && result.path.startsWith('assets/')) {
                        prefabResults.push(result);
                        if (prefabResults.length <= 5) {
                            self.show('宽松匹配prefab: ' + result.path + ' (type: ' + result.type + ')');
                        }
                    }
                }
                self.show('宽松匹配找到 ' + prefabResults.length + ' 个prefab文件');
            }
            
            if (prefabResults.length === 0) {
                callback([]);
                return;
            }
            
            // 按路径过滤
            self.filterPrefabsByPath(prefabResults, basePath, includeSubfolders, callback);
        });
    },

    tryFileSystemQuery(basePath, includeSubfolders, callback) {
        let self = this;
        
        self.show('尝试使用文件系统API查询...');
        
        // 使用Editor.assetdb.queryInfos尝试
        if (Editor.assetdb.queryInfos) {
            Editor.assetdb.queryInfos('**/*.prefab', function (err, infos) {
                if (err) {
                    self.show('文件系统查询失败: ' + err.message);
                    callback([]);
                    return;
                }
                
                if (!infos || infos.length === 0) {
                    self.show('文件系统查询无结果');
                    callback([]);
                    return;
                }
                
                self.show('文件系统查询找到 ' + infos.length + ' 个.prefab文件');
                
                // 转换为标准格式
                let results = infos.map(info => ({
                    uuid: info.uuid,
                    path: info.path,
                    type: 'cc.Prefab',
                    url: info.url
                }));
                
                self.filterPrefabsByPath(results, basePath, includeSubfolders, callback);
            });
        } else {
            self.show('文件系统API不可用');
            callback([]);
        }
    },

    filterPrefabsByPath(results, basePath, includeSubfolders, callback) {
        let self = this;
        
        // 过滤出在指定路径下的prefab
        let filteredResults = [];
        let targetPath = basePath.replace('db://', '');
        
        // 如果目标路径是assets，需要特殊处理
        if (targetPath === 'assets') {
            targetPath = '';
        }
        
        self.show('路径过滤，目标路径: "' + targetPath + '"');
        
        for (let result of results) {
            if (result.path) {
                let resultPath = result.path;
                
                // 记录每个文件的路径用于调试
                if (filteredResults.length < 5) { // 只显示前5个用于调试
                    self.show('检查文件: ' + resultPath);
                }
                
                let shouldInclude = false;
                
                if (targetPath === '') {
                    // 如果目标是根assets目录，包含所有文件
                    shouldInclude = true;
                } else if (includeSubfolders) {
                    // 递归搜索，检查路径是否以目标路径开头
                    shouldInclude = resultPath.startsWith(targetPath + '/') || resultPath === targetPath;
                } else {
                    // 非递归搜索，检查是否在直接子目录
                    let pathWithoutTarget = resultPath.substring(targetPath.length + 1);
                    shouldInclude = pathWithoutTarget && pathWithoutTarget.indexOf('/') === -1;
                }
                
                if (shouldInclude) {
                    filteredResults.push(result);
                    if (filteredResults.length <= 10) { // 显示前10个匹配文件
                        self.show('匹配文件: ' + resultPath);
                    }
                }
            }
        }
        
        self.show('路径过滤完成，找到 ' + filteredResults.length + ' 个匹配的Prefab文件');
        callback(filteredResults);
    },

    processPrefabResults(results, exportPath, exportResources) {
        let self = this;
        
        // 过滤掉编辑器内置资源，只保留项目prefab
        let projectPrefabs = results.filter(result => {
            if (!result.path) return false;
            
            // 排除编辑器内置资源
            let path = result.path.toLowerCase();
            let isBuiltIn = path.includes('default-assets') || 
                           path.includes('internal') || 
                           path.includes('creator/') || 
                           path.includes('programdata/') ||
                           path.includes('cocos/editors/');
                           
            return !isBuiltIn;
        });
        
        if (projectPrefabs.length === 0) {
            self.sendError('过滤后没有找到项目Prefab文件\n原始数量: ' + results.length + '\n' +
                          '可能原因:\n1. 所有找到的prefab都是编辑器内置资源\n2. 项目中没有自定义prefab文件');
            return;
        }
        
        self.show('过滤后找到 ' + projectPrefabs.length + ' 个项目Prefab文件，开始批量导出...');
        self.show('已过滤掉 ' + (results.length - projectPrefabs.length) + ' 个编辑器内置prefab');
        
        // 更新进度
        self.updateProgress(0, projectPrefabs.length);
        
        let exportedCount = 0;
        let failedCount = 0;
        let skippedCount = 0;
        let allResources = new Set(); // 收集所有资源
        
        const processNext = (index) => {
            if (index >= projectPrefabs.length) {
                // 所有文件处理完成
                let message = `批量导出完成！\n成功: ${exportedCount}个\n失败: ${failedCount}个\n跳过: ${skippedCount}个`;
                
                if (exportResources && allResources.size > 0) {
                    self.exportResourceList(Array.from(allResources), exportPath);
                    message += `\n已导出资源清单 (${allResources.size}个资源)`;
                }
                
                self.sendComplete(message);
                return;
            }

            let result = projectPrefabs[index];
            let prefabName = self.getPrefabNameFromPath(result.path);
            
            self.show(`导出进度 ${index + 1}/${projectPrefabs.length}: ${prefabName}`);
            self.updateProgress(index + 1, projectPrefabs.length);

            // 验证prefab是否可以处理
            if (!result.uuid || !result.path) {
                skippedCount++;
                self.show(`跳过无效prefab: ${prefabName} (缺少uuid或path)`);
                setTimeout(() => processNext(index + 1), 50);
                return;
            }

            try {
                self.exportPrefabWithCallback(result.uuid, exportPath, exportResources, (resources) => {
                    exportedCount++;
                    if (resources && exportResources) {
                        resources.forEach(res => allResources.add(res));
                    }
                    // 处理下一个
                    setTimeout(() => processNext(index + 1), 50); // 小延迟避免阻塞UI
                }, (error) => {
                    failedCount++;
                    self.show(`导出失败 ${prefabName}: ${error}`);
                    // 处理下一个
                    setTimeout(() => processNext(index + 1), 50);
                }, result.path); // 传递原始路径
            } catch (error) {
                failedCount++;
                self.show(`导出异常 ${prefabName}: ${error.message}`);
                setTimeout(() => processNext(index + 1), 50);
            }
        };

        // 显示前几个要处理的prefab用于确认
        self.show('即将处理的prefab示例:');
        for (let i = 0; i < Math.min(5, projectPrefabs.length); i++) {
            self.show(`  ${i + 1}. ${projectPrefabs[i].path}`);
        }
        if (projectPrefabs.length > 5) {
            self.show(`  ... 还有 ${projectPrefabs.length - 5} 个prefab`);
        }

        // 开始处理
        processNext(0);
    },

    getPrefabNameFromPath(path) {
        if (!path) return 'unknown';
        
        // 处理Windows和Unix路径分隔符
        let normalizedPath = path.replace(/\\/g, '/');
        let parts = normalizedPath.split('/');
        let filename = parts[parts.length - 1];
        
        // 移除.prefab扩展名
        return filename.replace('.prefab', '');
    },

    exportResourceList(resources, exportPath) {
        let resourceData = {
            exportTime: new Date().toISOString(),
            totalResources: resources.length,
            resources: resources.map(res => ({
                name: res.name,
                type: res.type,
                path: res.path
            }))
        };

        try {
            let filePath = Path.join(exportPath, 'resource_list.json');
            Fs.writeFileSync(filePath, JSON.stringify(resourceData, null, 2), 'utf8');
            this.show('资源清单已保存: ' + filePath);
        } catch (error) {
            this.show('保存资源清单失败: ' + error.message);
        }
    },

    updateProgress(current, total) {
        Editor.Ipc.sendToPanel('prefab-exporter', 'prefab-exporter:update-progress', current, total);
    },

    sendComplete(message) {
        Editor.Ipc.sendToPanel('prefab-exporter', 'prefab-exporter:export-complete', message);
    },

    sendError(error) {
        Editor.Ipc.sendToPanel('prefab-exporter', 'prefab-exporter:export-error', error);
    },

    exportPrefab(prefabUuid, exportPath, exportResources = false) {
        this.exportPrefabWithCallback(prefabUuid, exportPath, exportResources, 
            (resources) => {
                let message = 'Prefab导出成功!';
                if (resources && exportResources) {
                    message += `\n包含 ${resources.length} 个资源引用`;
                }
                this.sendComplete(message);
            },
            (error) => {
                this.sendError('导出失败: ' + error);
            }
        );
    },

    exportPrefabWithCallback(prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath) {
        let self = this;
        
        // 检查prefabUuid的有效性
        if (!prefabUuid) {
            if (errorCallback) errorCallback('prefabUuid为空');
            return;
        }
        
        // 尝试使用新的资源管理器API (Cocos Creator 2.4+)
        if (cc.assetManager && cc.assetManager.loadAny) {
            self.loadWithAssetManager(prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath);
        } else if (cc.loader) {
            // 回退到旧的cc.loader API
            self.loadWithLegacyLoader(prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath);
        } else {
            if (errorCallback) errorCallback('没有可用的资源加载API');
        }
    },

    loadWithAssetManager(prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath) {
        let self = this;
        
        self.show('使用AssetManager加载资源: ' + prefabUuid);
        
        cc.assetManager.loadAny({ uuid: prefabUuid }, function (err, asset) {
            if (err) {
                self.show('AssetManager加载失败: ' + err.message);
                // 尝试使用旧API
                if (cc.loader) {
                    self.loadWithLegacyLoader(prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath);
                } else if (errorCallback) {
                    errorCallback('AssetManager加载失败: ' + err.message);
                }
                return;
            }

            self.processLoadedPrefab(asset, prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath);
        });
    },

    loadWithLegacyLoader(prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath) {
        let self = this;
        
        self.show('使用cc.loader加载资源: ' + prefabUuid);
        
        // 使用旧版本的cc.loader API
        cc.loader.load({ type: 'uuid', uuid: prefabUuid }, function (err, res) {
            if (err) {
                self.show('cc.loader加载失败: ' + err.message);
                if (errorCallback) errorCallback('cc.loader加载失败: ' + err.message);
                return;
            }

            self.processLoadedPrefab(res, prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath);
        });
    },

    processLoadedPrefab(prefabAsset, prefabUuid, exportPath, exportResources, successCallback, errorCallback, originalPath) {
        let self = this;
        
        try {
            // 检查prefab资源是否有效
            if (!prefabAsset) {
                if (errorCallback) errorCallback('Prefab资源为空');
                return;
            }

            // 创建临时节点来获取完整数据
            let tempNode;
            try {
                tempNode = cc.instantiate(prefabAsset);
            } catch (instantiateError) {
                if (errorCallback) errorCallback('实例化Prefab失败: ' + instantiateError.message);
                return;
            }
            
            if (!tempNode) {
                if (errorCallback) errorCallback('实例化后节点为空');
                return;
            }
            
            let rootNode = tempNode;
            
            // 收集资源引用
            let resources = [];
            if (exportResources) {
                try {
                    resources = self.collectResources(rootNode);
                } catch (collectError) {
                    self.show('收集资源时出错: ' + collectError.message);
                    resources = [];
                }
            }
            
            // 转换为JSON数据
            let jsonData;
            try {
                jsonData = self.convertNodeToJson(rootNode);
            } catch (convertError) {
                if (errorCallback) errorCallback('转换JSON失败: ' + convertError.message);
                tempNode.destroy();
                return;
            }
            
            // 获取正确的prefab名称和路径
            let fileName, filePath;
            if (originalPath) {
                // 从原始路径提取文件名，保持目录结构
                fileName = self.getPrefabNameFromPath(originalPath);
                
                // 处理路径：提取assets之后的相对路径
                let relativePath;
                if (originalPath.includes('assets\\') || originalPath.includes('assets/')) {
                    // Windows或Unix路径中找到assets目录
                    let assetsIndex = originalPath.lastIndexOf('assets');
                    if (assetsIndex !== -1) {
                        relativePath = originalPath.substring(assetsIndex + 7); // 跳过'assets/'或'assets\'
                        relativePath = relativePath.replace(/\\/g, '/'); // 统一使用正斜杠
                        relativePath = relativePath.replace('.prefab', '.json');
                    } else {
                        // 回退：使用文件名
                        relativePath = fileName + '.json';
                    }
                } else {
                    // 如果路径不包含assets，直接使用文件名
                    relativePath = fileName + '.json';
                }
                
                filePath = Path.join(exportPath, relativePath);
                
                // 确保目录存在
                let dirPath = Path.dirname(filePath);
                self.ensureDirectoryExists(dirPath);
                
                self.show('处理路径: ' + originalPath + ' -> ' + relativePath);
            } else {
                // 回退方案：使用UUID作为文件名
                fileName = 'prefab_' + prefabUuid.substring(0, 8);
                filePath = Path.join(exportPath, fileName + '.json');
            }
            
            // 清理临时节点
            tempNode.destroy();
            
            // 写入文件
            try {
                Fs.writeFileSync(filePath, JSON.stringify(jsonData, null, 2), 'utf8');
                
                self.show('导出完成: ' + (originalPath ? Path.relative(exportPath, filePath) : fileName + '.json'));
                
                if (successCallback) {
                    successCallback(resources);
                }
            } catch (writeError) {
                if (errorCallback) errorCallback('写入文件失败: ' + writeError.message);
            }
            
        } catch (error) {
            if (errorCallback) {
                errorCallback('处理Prefab时出错: ' + error.message);
            }
            self.show('处理Prefab失败: ' + error.message);
        }
    },

    ensureDirectoryExists(dirPath) {
        try {
            if (!Fs.existsSync(dirPath)) {
                // 递归创建目录
                let parentDir = Path.dirname(dirPath);
                if (parentDir !== dirPath && parentDir !== '.') {
                    this.ensureDirectoryExists(parentDir);
                }
                
                // 验证路径是否有效
                if (dirPath.length < 3 || dirPath.includes(':') && dirPath.indexOf(':') > 1) {
                    throw new Error('无效的目录路径: ' + dirPath);
                }
                
                Fs.mkdirSync(dirPath);
                this.show('创建目录: ' + dirPath);
            }
        } catch (error) {
            this.show('创建目录失败: ' + dirPath + ', 错误: ' + error.message);
            throw error;
        }
    },

    collectResources(node) {
        let resources = [];
        let self = this;

        const collectFromNode = (node) => {
            if (!node) return;

            // 收集精灵资源
            let sprite = node.getComponent('cc.Sprite') || node.getComponent(cc.Sprite);
            if (sprite && sprite.spriteFrame) {
                resources.push({
                    name: sprite.spriteFrame.name || '',
                    type: 'SpriteFrame',
                    path: sprite.spriteFrame._texture ? (sprite.spriteFrame._texture.nativeUrl || sprite.spriteFrame._texture.url) : '',
                    uuid: sprite.spriteFrame._uuid
                });

                // 收集图集纹理
                if (sprite.spriteFrame._texture) {
                    resources.push({
                        name: sprite.spriteFrame._texture.name || '',
                        type: 'Texture2D',
                        path: sprite.spriteFrame._texture.nativeUrl || sprite.spriteFrame._texture.url || '',
                        uuid: sprite.spriteFrame._texture._uuid
                    });
                }
            }

            // 收集字体资源
            let label = node.getComponent('cc.Label') || node.getComponent(cc.Label);
            if (label && label.font) {
                resources.push({
                    name: label.font.name || '',
                    type: label.font instanceof cc.BitmapFont ? 'BitmapFont' : 'Font',
                    path: label.font.nativeUrl || label.font.url || '',
                    uuid: label.font._uuid
                });
            }

            // 递归处理子节点
            if (node.children) {
                for (let child of node.children) {
                    collectFromNode(child);
                }
            }
        };

        collectFromNode(node);
        
        // 去重
        let uniqueResources = [];
        let seenUuids = new Set();
        
        for (let resource of resources) {
            if (resource.uuid && !seenUuids.has(resource.uuid)) {
                seenUuids.add(resource.uuid);
                uniqueResources.push(resource);
            }
        }

        return uniqueResources;
    },

    convertNodeToJson(node) {
        if (!node) return null;

        let nodeData = {};
        
        // 基本属性
        nodeData.name = node.name || 'Node';
        nodeData.active = node.active;
        
        // 位置信息
        nodeData.pos = {
            x: Math.round(node.x * 100) / 100,
            y: Math.round(node.y * 100) / 100,
            z: Math.round((node.z || 0) * 100) / 100
        };
        
        // 缩放信息
        nodeData.scale = {
            x: Math.round(node.scaleX * 100) / 100,
            y: Math.round(node.scaleY * 100) / 100,
            z: Math.round((node.scaleZ || 1) * 100) / 100
        };
        
        // 旋转信息 - 使用新的angle API替代废弃的rotation
        let rotationValue = 0;
        if (typeof node.angle !== 'undefined') {
            rotationValue = -node.angle; // 新API使用负值
        } else if (typeof node.rotation !== 'undefined') {
            rotationValue = node.rotation; // 兼容旧API
        }
        nodeData.rotation = Math.round(rotationValue * 100) / 100;
        
        // 尺寸信息
        let size = node.getContentSize();
        nodeData.size = {
            width: Math.round(size.width),
            height: Math.round(size.height)
        };
        
        // 锚点信息
        nodeData.anchor = {
            x: Math.round(node.anchorX * 100) / 100,
            y: Math.round(node.anchorY * 100) / 100
        };
        
        // 转换组件
        let components = [];
        let hasButton = false;
        let scrollViewData = null;
        let gridData = null;
        
        // 遍历组件 - 兼容不同版本的API
        let nodeComponents = [];
        try {
            nodeComponents = node.getComponents(cc.Component);
        } catch (e) {
            // 如果getComponents失败，尝试使用_components属性
            if (node._components) {
                nodeComponents = node._components;
            }
        }
        
        for (let i = 0; i < nodeComponents.length; i++) {
            let comp = nodeComponents[i];
            let compData = this.convertComponent(comp, node);
            if (compData) {
                if (compData.type === 'UIButton') {
                    hasButton = true;
                } else if (compData.type === 'UIScrollView') {
                    scrollViewData = compData.data;
                } else if (compData.type === 'UIGrid') {
                    gridData = compData.data;
                } else {
                    components.push(compData);
                }
            }
        }
        
        if (components.length > 0) {
            nodeData.components = components;
        }
        
        if (hasButton) {
            nodeData.button = true;
        }
        
        if (scrollViewData) {
            nodeData.scrollView = scrollViewData;
        }
        
        if (gridData) {
            nodeData.grid = gridData;
        }
        
        // 处理子节点
        if (node.children && node.children.length > 0) {
            let children = [];
            for (let i = 0; i < node.children.length; i++) {
                let child = node.children[i];
                children.push(this.convertNodeToJson(child));
            }
            nodeData.children = children;
        }
        
        return nodeData;
    },

    convertComponent(component, node) {
        if (!component) return null;
        
        let compType = component.constructor.name;
        let size = node.getContentSize();
        
        // 基础组件数据
        let baseData = {
            size: {
                width: Math.round(size.width),
                height: Math.round(size.height)
            },
            color: this.colorToHex(node.color),
            pivot: getPivotFromAnchor(node.anchorX, node.anchorY),
            depth: component.node ? component.node.getSiblingIndex() : 0
        };
        
        switch (compType) {
            case 'Sprite':
            case 'cc.Sprite':
                return this.convertSprite(component, baseData);
            case 'Label':
            case 'cc.Label':
                return this.convertLabel(component, baseData);
            case 'Button':
            case 'cc.Button':
                return { type: 'UIButton' };
            case 'ScrollView':
            case 'cc.ScrollView':
                return this.convertScrollView(component, baseData, node);
            case 'Layout':
            case 'cc.Layout':
                return this.convertLayout(component, baseData);
            case 'Widget':
            case 'cc.Widget':
                return this.convertWidget(component, baseData);
            default:
                // 忽略系统组件
                if (compType === 'Transform' || compType === 'cc.Transform' || 
                    compType.includes('RenderComponent') || compType.includes('_SGComponent')) {
                    return null;
                }
                // 未知组件类型，创建基础UIWidget
                return {
                    type: 'UIWidget',
                    ...baseData
                };
        }
    },

    convertSprite(sprite, baseData) {
        let spriteData = {
            type: 'UISprite',
            ...baseData
        };
        
        // 精灵帧信息
        if (sprite.spriteFrame) {
            spriteData.spName = sprite.spriteFrame.name || '';
            
            // 尝试获取图集名称 - 在Cocos Creator中通过texture获取
            if (sprite.spriteFrame._texture) {
                let textureName = sprite.spriteFrame._texture.name;
                if (textureName) {
                    // 移除文件扩展名
                    spriteData.atlas = textureName.replace(/\.[^/.]+$/, '');
                }
            }
        }
        
        // 精灵类型
        if (sprite.type !== undefined) {
            switch (sprite.type) {
                case cc.Sprite.Type.SIMPLE:
                    spriteData.spType = 'Simple';
                    break;
                case cc.Sprite.Type.SLICED:
                    spriteData.spType = 'Sliced';
                    break;
                case cc.Sprite.Type.TILED:
                    spriteData.spType = 'Tiled';
                    break;
                case cc.Sprite.Type.FILLED:
                    spriteData.spType = 'Filled';
                    if (sprite.fillType !== undefined) {
                        spriteData.fillDir = sprite.fillType;
                    }
                    break;
            }
        }
        
        // 边框信息 - 从spriteFrame获取
        if (sprite.spriteFrame && sprite.spriteFrame.insetTop !== undefined) {
            let border = {
                left: sprite.spriteFrame.insetLeft || 0,
                right: sprite.spriteFrame.insetRight || 0,
                top: sprite.spriteFrame.insetTop || 0,
                bottom: sprite.spriteFrame.insetBottom || 0
            };
            
            // 只有在有边框数据时才添加
            if (border.left > 0 || border.right > 0 || border.top > 0 || border.bottom > 0) {
                spriteData.border = border;
            }
        }
        
        return spriteData;
    },

    convertLabel(label, baseData) {
        let labelData = {
            type: 'UILabel',
            ...baseData,
            text: label.string || '',
            fontSize: label.fontSize || 40
        };
        
        // 溢出模式
        if (label.overflow !== undefined) {
            switch (label.overflow) {
                case cc.Label.Overflow.NONE:
                    labelData.overflow = 'ResizeFreely';
                    break;
                case cc.Label.Overflow.CLAMP:
                    labelData.overflow = 'ClampContent';
                    break;
                case cc.Label.Overflow.SHRINK:
                    labelData.overflow = 'ShrinkContent';
                    break;
                case cc.Label.Overflow.RESIZE_HEIGHT:
                    labelData.overflow = 'ResizeHeight';
                    break;
            }
        }
        
        // 字体信息
        if (label.font) {
            if (label.font instanceof cc.BitmapFont) {
                labelData.bitmapFont = label.font.name || '';
                labelData.spacingX = label.spacingX || 0;
            }
        }
        
        // 行高转换为spacingY
        if (label.lineHeight && label.fontSize) {
            labelData.spacingY = Math.max(0, label.lineHeight - label.fontSize);
        }
        
        // 检查描边组件 - 兼容不同版本的API
        let outline = null;
        try {
            outline = label.getComponent('cc.LabelOutline') || label.getComponent(cc.LabelOutline);
        } catch (e) {
            // 如果获取描边组件失败，尝试其他方式
            let components = label.node.getComponents(cc.Component);
            for (let comp of components) {
                if (comp.constructor.name === 'LabelOutline' || comp.constructor.name === 'cc.LabelOutline') {
                    outline = comp;
                    break;
                }
            }
        }
        
        if (outline) {
            labelData.outlineColor = this.colorToHex(outline.color);
            labelData.outlineWidth = outline.width || 1;
        }
        
        return labelData;
    },

    convertScrollView(scrollView, baseData, node) {
        let scrollData = {
            offset: {
                x: node.x || 0,
                y: node.y || 0
            },
            size: {
                x: baseData.size.width,
                y: baseData.size.height
            }
        };
        
        // 移动方向
        if (scrollView.horizontal && scrollView.vertical) {
            scrollData.movement = 2; // Unrestricted
        } else if (scrollView.horizontal) {
            scrollData.movement = 0; // Horizontal
        } else if (scrollView.vertical) {
            scrollData.movement = 1; // Vertical
        } else {
            scrollData.movement = 3; // Custom
        }
        
        return {
            type: 'UIScrollView',
            data: scrollData
        };
    },

    convertLayout(layout, baseData) {
        let gridData = {
            arrangement: 0 // 默认水平排列
        };
        
        if (layout.type !== undefined) {
            switch (layout.type) {
                case cc.Layout.Type.HORIZONTAL:
                    gridData.arrangement = 0;
                    break;
                case cc.Layout.Type.VERTICAL:
                    gridData.arrangement = 1;
                    break;
                case cc.Layout.Type.GRID:
                    gridData.arrangement = 2; // CellSnap
                    break;
            }
        }
        
        return {
            type: 'UIGrid',
            data: gridData
        };
    },

    convertWidget(widget, baseData) {
        return {
            type: 'UIWidget',
            ...baseData
        };
    },

    colorToHex(color) {
        if (!color) return 'FFFFFF';
        
        let r = Math.round(color.r * 255).toString(16).padStart(2, '0');
        let g = Math.round(color.g * 255).toString(16).padStart(2, '0');
        let b = Math.round(color.b * 255).toString(16).padStart(2, '0');
        
        return (r + g + b).toUpperCase();
    },

    // main.js中可以打印出完整结构
    show(any) {
        Editor.Ipc.sendToMain('prefab-exporter:show', any);
    }
};
