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

// Pivot映射
const PivotMap = {
    0: 'BottomLeft',    // 左下
    1: 'Bottom',        // 下
    2: 'BottomRight',   // 右下
    3: 'Left',          // 左
    4: 'Center',        // 中
    5: 'Right',         // 右
    6: 'TopLeft',       // 左上
    7: 'Top',           // 上
    8: 'TopRight'       // 右上
};

module.exports = {
    'export': function (event, info) {
        this.show('开始导出Prefab: ' + info.prefab);
        
        if (!info.prefab) {
            Editor.Dialog.messageBox({
                title: '错误',
                type: 'error',
                message: '请选择Prefab文件!'
            });
            return;
        }

        this.exportPrefab(info.prefab, info.exportPath);
    },

    exportPrefab(prefabUuid, exportPath) {
        let self = this;
        
        // 加载prefab资源
        cc.loader.load({ type: 'uuid', uuid: prefabUuid }, function (err, res) {
            if (err) {
                Editor.Dialog.messageBox({
                    title: '错误',
                    type: 'error',
                    message: '加载Prefab失败: ' + err.message
                });
                return;
            }

            try {
                // 获取prefab的根节点
                let prefabNode = res.data;
                if (!prefabNode) {
                    Editor.Dialog.messageBox({
                        title: '错误',
                        type: 'error',
                        message: 'Prefab数据为空!'
                    });
                    return;
                }

                // 转换为JSON数据
                let jsonData = self.convertNodeToJson(prefabNode);
                
                // 写入文件
                let fileName = prefabNode.name || 'exported_prefab';
                let filePath = Path.join(exportPath, fileName + '.json');
                
                Fs.writeFileSync(filePath, JSON.stringify(jsonData, null, 2), 'utf8');
                
                Editor.Dialog.messageBox({
                    title: '成功',
                    type: 'info',
                    message: 'Prefab导出成功!\n路径: ' + filePath
                });
                
                self.show('导出完成: ' + filePath);
            } catch (error) {
                Editor.Dialog.messageBox({
                    title: '错误',
                    type: 'error',
                    message: '导出失败: ' + error.message
                });
            }
        });
    },

    convertNodeToJson(node) {
        let nodeData = {};
        
        // 基本属性
        nodeData.name = node.name;
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
        
        // 旋转信息
        nodeData.rotation = Math.round(node.rotation * 100) / 100;
        
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
        
        // 遍历组件
        for (let comp of node._components) {
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
            for (let child of node.children) {
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
            pivot: this.getUIPivotFromAnchor(node.anchorX, node.anchorY)
        };
        
        switch (compType) {
            case 'cc.Sprite':
                return this.convertSprite(component, baseData);
            case 'cc.Label':
                return this.convertLabel(component, baseData);
            case 'cc.Button':
                return { type: 'UIButton' };
            case 'cc.ScrollView':
                return this.convertScrollView(component, baseData, node);
            case 'cc.Layout':
                return this.convertLayout(component, baseData);
            case 'cc.Widget':
                return this.convertWidget(component, baseData);
            default:
                // 未知组件类型，创建基础UIWidget
                if (compType !== 'cc.Transform' && compType !== 'cc.RenderComponent') {
                    return {
                        type: 'UIWidget',
                        ...baseData,
                        depth: component.zOrder || 0
                    };
                }
                return null;
        }
    },

    convertSprite(sprite, baseData) {
        let spriteData = {
            type: 'UISprite',
            ...baseData,
            depth: sprite.zOrder || 0
        };
        
        // 精灵帧信息
        if (sprite.spriteFrame) {
            spriteData.spName = sprite.spriteFrame.name;
            // 尝试获取图集名称
            if (sprite.spriteFrame._texture && sprite.spriteFrame._texture.url) {
                let texturePath = sprite.spriteFrame._texture.url;
                let pathParts = texturePath.split('/');
                if (pathParts.length > 0) {
                    spriteData.atlas = pathParts[pathParts.length - 1].replace(/\.[^/.]+$/, '');
                }
            }
        }
        
        // 精灵类型
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
                spriteData.fillDir = sprite.fillType;
                break;
        }
        
        return spriteData;
    },

    convertLabel(label, baseData) {
        let labelData = {
            type: 'UILabel',
            ...baseData,
            depth: label.zOrder || 0,
            text: label.string || '',
            fontSize: label.fontSize || 40
        };
        
        // 溢出模式
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
        
        // 字体信息
        if (label.font) {
            if (label.font instanceof cc.BitmapFont) {
                labelData.bitmapFont = label.font.name;
                labelData.spacingX = label.spacingX || 0;
            }
        }
        
        // 行高转换为spacingY
        if (label.lineHeight && label.fontSize) {
            labelData.spacingY = Math.max(0, label.lineHeight - label.fontSize);
        }
        
        return labelData;
    },

    convertScrollView(scrollView, baseData, node) {
        let scrollData = {
            offset: {
                x: node.x,
                y: node.y
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
        
        return {
            type: 'UIGrid',
            data: gridData
        };
    },

    convertWidget(widget, baseData) {
        return {
            type: 'UIWidget',
            ...baseData,
            depth: widget.zOrder || 0
        };
    },

    getUIPivotFromAnchor(anchorX, anchorY) {
        // 根据锚点计算NGUI的Pivot
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
    },

    colorToHex(color) {
        if (!color) return 'FFFFFF';
        
        let r = Math.round(color.r).toString(16).padStart(2, '0');
        let g = Math.round(color.g).toString(16).padStart(2, '0');
        let b = Math.round(color.b).toString(16).padStart(2, '0');
        
        return (r + g + b).toUpperCase();
    },

    // main.js中可以打印出完整结构
    show(any) {
        Editor.Ipc.sendToMain('prefab-exporter:show', any);
    }
};
