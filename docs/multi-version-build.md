# 塔什干双版本构建

在 Rider 打开 `TashkentSpire2.Impl.sln`，切换到 **Release**，执行 **Build Solution**。项目和解决方案沿用 Godot 依据程序集名查找文件的规则。命令行使用同一个 MSBuild 目标：

```powershell
dotnet build TashkentSpire2.Impl.sln -c Release
```

Build 完成后自动把整套双版本包安装到游戏的 `mods/TashkentSpire2/`。构建也会保留一份干净的成品，Release 位于 `.artifacts/Release/TashkentSpire2/`，Debug 位于 `.artifacts/Debug/TashkentSpire2/`，工坊上传时使用这里的成品。可在 `Directory.Build.props` 通过 `ModsPath` 指定其他游戏的 mods 目录，或设置 `DeployBundle=false` 仅打包。两版游戏都需要另外安装各自兼容的 BaseLib 3.4.8 或更高版本。

```text
TashkentSpire2/
├── mod_manifest.json
├── TashkentSpire2.dll
└── bin/
    ├── g0.111.0/
    │   ├── TashkentSpire2.Impl.dll
    │   └── TashkentSpire2.pck
    └── g0.113.1/
        ├── TashkentSpire2.Impl.dll
        └── TashkentSpire2.pck
```

## 构建环境

- .NET 9 SDK、Git 和 `tar`。Windows 默认使用系统自带的 Windows PowerShell 5.1，Linux 和 macOS 使用 PowerShell 7（`pwsh`）。如需指定其他安装，可以在 `Directory.Build.props` 中设置 `PowerShellPath`。
- Godot **4.5.1 Mono**。Windows 使用 `_console.exe` 以便捕获导出日志。保留项目 `bin/` 中与编辑器平台匹配的 Spine GDExtension 原生库。
- 已安装 0.113.1，包含游戏根目录的 `release_info.json`，以及 `data_sts2_windows_x86_64` 内的游戏引用程序集。
- 一份 0.111.0 原版的引用程序集目录。Windows 通常为旧版游戏的 `data_sts2_windows_x86_64`。至少需要该版本的 `sts2.dll`、`0Harmony.dll` 和 `Steamworks.NET.dll`。

当前 Windows 工作区的 Godot 和 0.113.1 安装使用默认路径。将旧版程序集目录写入项目根目录的本地 `Directory.Build.props`，Rider 和命令行共用这些设置：

```xml
<Project>
  <PropertyGroup>
    <GodotPath>D:/Godot/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe</GodotPath>
    <Sts2Path>D:/STEAM/steamapps/common/Slay the Spire 2</Sts2Path>
    <StableSts2DataDir>D:/Games/STS2-0.111.0/data_sts2_windows_x86_64</StableSts2DataDir>
  </PropertyGroup>
</Project>
```

`Sts2DataDir` 可以覆盖新版本的引用程序集目录。只检查当前实现的 C# 编译时可加 `-p:BuildBundle=false`。Godot 导出日志在 `.artifacts/<配置>/godot-g<游戏版本>.log`。

## 更新两套实现

- **0.113.1** 使用当前打开的 Git 工作区，包含未提交的源码和资源修改。若另开 `branch_tashkent_beta`，从该分支的工作区执行相同命令。
- **0.111.0** 默认从 Git 提交 `9234a743ea19d750b4adee6ee6c2be1a0295b904` 取源码和资源，并在生成的快照中应用 `build/compat/g0.111.0-sync-hooks.patch`。维护旧版后，在 `Directory.Build.props` 设置 `StableSourceRef`，或在命令行传 `-p:StableSourceRef=<已提交的分支、标签或提交>`。打包会读取该 ref 指向的提交，当前分支无需切换。
- 修改根目录 `mod_manifest.json` 的 `version` 就能统一启动器和两套实现的程序集版本。`min_game_version` 用于游戏原生加载检查，应保持为包所声明支持的最低游戏版本。

每个 PCK 由对应源码快照的 `BasicExport` 配置通过 Godot `--headless --export-pack` 生成。版本选择使用游戏的 `release_info.json`，依次尝试精确命中、不高于当前游戏的最高版本、`bin/latest`，读取不到游戏版本时再尝试最高的数字版本目录。DLL 和 PCK 始终从同一目录加载，缺文件会报出具体目录。`has_pck` 设为 `false`，由启动器先挂载选中 PCK、再调用实现入口。

当前的两个构建目标是 0.111.0 和 0.113.1。按回退规则，0.113.0 会尝试 `g0.111.0`，这种选择结果只说明加载路径，兼容性需要单独验证。

## 更新现有工坊条目

本机上传器 GUI 的文件快照只收集根目录的同名 DLL、PCK 和 JSON。双版本包可用同一上传器的原始 CLI 上传整个目录。

1. 在上传工作区放一个根级 `workshop.json`（仅更新内容时可用 `{}`）。
2. 把成品目录中的 `mod_manifest.json`、启动器 DLL 和整个 `bin/` 放到该工作区的 `content/` 下。
3. 登录发布塔什干的 Steam 账号后，显式指定现有条目和账号执行：

```powershell
$publisherSteamId = Read-Host '发布账号的 SteamID64'
& 'D:/GitClone/sts2-mod-uploader-main/app/worker/ModUploader.exe' upload --workspace 'D:/路径/到上传工作区' --id 3747818292 --content-only --expected-steam-id $publisherSteamId
```

这条命令保留原条目的简介、可见性和 Steam 游戏版本限制。如果工坊条目之前为 Beta 单独设置了版本限制，也需要在发布时核对该限制是否覆盖两个目标版本。
