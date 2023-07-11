# 概览
升级Windows11后没有了右下角工具栏功能了，想要执行一些工具脚本非常不方便，所以就自己撸了一个小工具，启动后会在系统托盘常驻，右键即可运行相应的脚本或者程序。

目前支持直接运行下面后缀的文件：

- ps1
  - 优先使用`pwsh.exe`运行，如果没有安装[新版本PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows)那么会使用旧版`powershell.exe`运行
- 其他任意文件
  - 用系统关联的程序打开


# 配置

工具启动的时候会读取同路径下的`ToolTray.ini`文件，里面配置了相应脚本工具的文件夹路径，参考如下：

```ini
; 显示的顶层名字
[ToolBox1]
; 脚本工具所在的文件夹，如果不存在会忽略
PATH=\path\to\folder1
; 是否递归查找，1是启动递归，其他值或者不指定那么不会递归查找
RECURSE=1
; 文件过滤器，默认是*所有文件，用半角逗号可以指定多个
FILTER=*.bat,*.vbs
; 设置需要管理员权限运行的文件过滤器，格式跟FILTER一样
ADMIN=*.ps1
; 顶层工具文件夹名字可以指定多个
[ToolBox2]
; 脚本工具所在的文件夹，支持嵌入%环境变量%
PATH=%OneDrive%\folder2
; 是否递归查找，1是启动递归，其他值或者不指定那么不会递归查找
RECURSE=0
; 文件过滤器，默认是*所有文件，用半角逗号可以指定多个
FILTER=*.ps1
; 设置需要管理员权限运行的文件过滤器，格式跟FILTER一样
ADMIN=ADMIN.ps1
```

可以单击托盘图标弹出主窗口编辑或者刷新配置文件。

# 开机启动

`win+r`运行`shell:Startup`打开开机启动文件夹，然后把程序的快捷键方式添加进去即可。
