# 时序小织匠 · 四时织坊

一个围绕四季节气与蚕桑丝织技艺设计的 Unity 游戏。仓库保留当前可编辑工程，团队可通过提交记录查看后续修改。

## 打开工程

1. 安装 Git LFS，运行 `git lfs install`。
2. 运行 `git clone https://github.com/lucinwen/4seasons.git`。
3. 在 Unity Hub 中添加克隆得到的 `4seasons` 文件夹。工程使用 Unity **2022.3.62f3c1**，以 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 为准。
4. 首次导入完成后，打开 [FourSeasonsTitle.unity](Assets/Scenes/FourSeasonsTitle.unity)，点击 Play。

## 工程内容

- `Assets/` 保存代码、场景、资源和 Unity `.meta` 文件。
- `Packages/` 和 `ProjectSettings/` 保存依赖与项目配置。
- `docs/wardrobe/` 保存成衣展架的预览总览与场景参考。可编辑的服装图集已在 `Assets/` 中。
- Git LFS 管理图片、音频和字体。Unity 首次打开时会重新生成本地缓存。

日常更新直接修改工程文件并提交。主要改动记录见 [CHANGELOG.md](CHANGELOG.md)。
