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
        
        // 在Cocos Creator 1.10.x/2.x中使用cc.loader加载资源
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
                // 获取prefab的根节点数据
                let prefabData = res;
                if (!prefabData) {
                    Editor.Dialog.messageBox({
                        title: '错误',
                        type: 'error',
                        message: 'Prefab数据为空!'
                    });
                    return;
                }

                // 创建临时节点来获取完整数据
                let tempNode = cc.instantiate(res);
                let rootNode = tempNode;
                
                // 转换为JSON数据
                let jsonData = self.convertNodeToJson(rootNode);
                
                // 清理临时节点
                tempNode.destroy();
                
                // 写入文件
                let fileName = rootNode.name || 'exported_prefab';
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
                self.show('导出失败: ' + error.message);
            }
        });
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
