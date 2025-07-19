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

    <h3>选择Prefab</h3>
	<div style="display:flex;">
		<ui-asset style="flex:1;" v-value="prefab" type="cc.Prefab"></ui-asset>   
		<ui-button class="transparent" @confirm="onExportClicked"><i class="fa fa-download"></i></ui-button>
	</div>
	
    <br/>

	<div style="font-size: 14px; cursor:pointer;" @click="gitHub">
        <i class="fa fa-github"> https://github.com/glegoo/ngui-cocos-creator-convertor </i>
    </div>

  `,

	// method executed when template and styles are successfully loaded and initialized
	ready() {
		new window.Vue({
			el: this.shadowRoot,
			data: {
				prefab: "",
				exportPath: "",
			},
			created: function () {
				// 默认导出到桌面
				this.exportPath = require('os').homedir() + '/Desktop';
			},
			methods: {
				onExportClicked(event) {
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
						exportPath: this.exportPath
					});
				},

				onPathClicked(event) {
					let path = this._openSelectFolder(this.exportPath);
					if (path) {
						this.exportPath = path;
					}
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
		'prefab-exporter:hello'(event) {
			// this.$node.innerText = 'Hello!';
			// Editor.log(this.$node.name)
		}
	}
});
