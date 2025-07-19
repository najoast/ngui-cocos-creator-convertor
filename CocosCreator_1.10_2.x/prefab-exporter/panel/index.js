const cp = require('child_process');
const Fs = require('fs');
const Path = require('path');

// panel/index.js, this filename needs to match the one registered in package.json
Editor.Panel.extend({
	// css style for panel
	style: `
    @import url('app://bower_components/fontawesome/css/font-awesome.min.css');
    h2 { color: #f90; }
  `,

	// html template for panel
	template: `
	<title>Prefab导出工具</title>
    <hr />
    
    <h3>导出路径</h3>
	<div style="display:flex;">
    <ui-input class="flex-1" placeholder="路径..." readonly v-value="exportPath"></ui-input>
	<ui-button class="transparent" @confirm="onPathClicked"><i class="fa fa-folder-open"></i></ui-button>
	</div>

    <h3>导出模式</h3>
    <ui-select v-value="exportMode" @confirm="onModeChanged">
        <option value="single">单个Prefab</option>
        <option value="batch">批量导出</option>
    </ui-select>

    <div v-show="exportMode === 'single'">
        <h3>选择Prefab</h3>
        <div style="display:flex;">
            <ui-asset style="flex:1;" v-value="prefab" type="cc.Prefab"></ui-asset>   
            <ui-button class="transparent" @confirm="onExportClicked"><i class="fa fa-download"></i></ui-button>
        </div>
    </div>

    <div v-show="exportMode === 'batch'">
        <h3>Prefab目录</h3>
        <div style="display:flex;">
            <ui-input class="flex-1" placeholder="选择包含Prefab的目录..." readonly v-value="prefabFolder"></ui-input>
            <ui-button class="transparent" @confirm="onPrefabFolderClicked"><i class="fa fa-folder-open"></i></ui-button>
        </div>
        
        <h3>批量导出选项</h3>
        <ui-checkbox v-value="includeSubfolders">包含子文件夹</ui-checkbox>
        <ui-checkbox v-value="exportResources">同时导出资源清单</ui-checkbox>
        
        <div style="margin-top: 10px;">
            <ui-button class="blue" @confirm="onBatchExportClicked" :disabled="!prefabFolder">
                <i class="fa fa-download"></i> 批量导出
            </ui-button>
            <span v-if="exportProgress.total > 0" style="margin-left: 10px;">
                进度: {{exportProgress.current}}/{{exportProgress.total}}
            </span>
        </div>
    </div>
	
    <br/>

	<div style="font-size: 14px; cursor:pointer;" @click="gitHub">
        <i class="fa fa-github"> https://github.com/glegoo/ngui-cocos-creator-convertor </i>
    </div>

  `,

	// method executed when template and styles are successfully loaded and initialized
	ready() {
		this.$vue = new window.Vue({
			el: this.shadowRoot,
			data: {
				prefab: "",
				exportPath: "",
				exportMode: "single",
				prefabFolder: "",
				includeSubfolders: true,
				exportResources: true,
				exportProgress: {
					current: 0,
					total: 0
				}
			},
			created: function () {
				// 默认导出到桌面
				this.exportPath = require('os').homedir() + '/Desktop';
			},
			methods: {
				onModeChanged() {
					// 切换模式时重置进度
					this.exportProgress.current = 0;
					this.exportProgress.total = 0;
				},

				onExportClicked(event) {
					if (this.exportMode === 'single') {
						this.exportSingle();
					}
				},

				onBatchExportClicked(event) {
					if (this.exportMode === 'batch') {
						this.exportBatch();
					}
				},

				exportSingle() {
					if (!this.prefab) {
						Editor.Dialog.messageBox({
							title: '错误',
							type: 'error',
							message: '请选择要导出的Prefab文件!'
						});
						return;
					}

					if (!this.exportPath) {
						Editor.Dialog.messageBox({
							title: '错误',
							type: 'error',
							message: '请选择导出路径!'
						});
						return;
					}

					Editor.Scene.callSceneScript('prefab-exporter', 'export', {
						prefab: this.prefab,
						exportPath: this.exportPath,
						exportResources: this.exportResources
					});
				},

				exportBatch() {
					if (!this.prefabFolder) {
						Editor.Dialog.messageBox({
							title: '错误',
							type: 'error',
							message: '请选择包含Prefab的目录!'
						});
						return;
					}

					if (!this.exportPath) {
						Editor.Dialog.messageBox({
							title: '错误',
							type: 'error',
							message: '请选择导出路径!'
						});
						return;
					}

					Editor.Scene.callSceneScript('prefab-exporter', 'batchExport', {
						prefabFolder: this.prefabFolder,
						exportPath: this.exportPath,
						includeSubfolders: this.includeSubfolders,
						exportResources: this.exportResources
					});
				},

				onPathClicked(event) {
					let path = this._openSelectFolder(this.exportPath);
					if (path) {
						this.exportPath = path;
					}
				},

				onPrefabFolderClicked(event) {
					let path = this._openSelectProjectFolder();
					if (path) {
						this.prefabFolder = path;
					}
				},

				_openSelectProjectFolder() {
					// 使用资源数据库选择项目内的文件夹
					let selectPath = Editor.Dialog.openFile({
						defaultPath: Editor.Project.path,
						properties: ['openDirectory'],
						title: '请选择Prefab目录'
					});

					if (selectPath !== -1) {
						let projectPath = Editor.Project.path.replace(/\\/g, '/');
						let selectedPath = String(selectPath).replace(/\\/g, '/');
						
						if (selectedPath.indexOf(projectPath) === 0) {
							// 转换为项目相对路径
							let relativePath = selectedPath.substring(projectPath.length);
							
							// 移除开头的斜杠（如果有）
							if (relativePath.startsWith('/')) {
								relativePath = relativePath.substring(1);
							}
							
							// 处理特殊情况
							if (relativePath === '' || relativePath === 'assets') {
								// 选择了项目根目录或assets目录，都映射到db://assets
								return 'db://assets';
							} else if (relativePath.startsWith('assets/')) {
								// 选择了assets下的子目录，移除assets前缀
								relativePath = relativePath.substring(7); // 移除'assets/'
								return 'db://assets/' + relativePath;
							} else {
								// 选择了项目下的其他目录（非assets），这种情况很少见
								return 'db://' + relativePath;
							}
						} else {
							Editor.Dialog.messageBox({
								title: '错误',
								type: 'error',
								message: '请选择项目assets目录下的文件夹!'
							});
							return null;
						}
					}
					return null;
				},

				_openSelectFolder(defaultPath) {
					let selectPath = Editor.Dialog.openFile({
						defaultPath: defaultPath,
						properties: ['openDirectory'],
						title: '请选择导出路径'
					});

					if (selectPath !== -1) {
						return String(selectPath);
					}
					return null;
				},

				gitHub() {
					cp.exec('start https://github.com/glegoo/ngui-cocos-creator-convertor');
				}
			}
		})
	},

	// register your ipc messages here
	messages: {
		'prefab-exporter:update-progress'(event, current, total) {
			if (this.$vue) {
				this.$vue.exportProgress.current = current;
				this.$vue.exportProgress.total = total;
			}
		},

		'prefab-exporter:export-complete'(event, message) {
			if (this.$vue) {
				this.$vue.exportProgress.current = 0;
				this.$vue.exportProgress.total = 0;
			}
			Editor.Dialog.messageBox({
				title: '完成',
				type: 'info',
				message: message
			});
		},

		'prefab-exporter:export-error'(event, error) {
			if (this.$vue) {
				this.$vue.exportProgress.current = 0;
				this.$vue.exportProgress.total = 0;
			}
			Editor.Dialog.messageBox({
				title: '错误',
				type: 'error',
				message: error
			});
		}
	}
});
