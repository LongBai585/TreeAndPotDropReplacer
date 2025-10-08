# TreeAndPotDropReplacer

一个TShock插件，用于替换泰拉瑞亚中摇树和砸罐子的掉落物，有概率生成巨石弹幕。

功能特性

· 🌳 替换摇树的掉落物为巨石弹幕
· 🏺 替换砸罐子的掉落物为巨石弹幕
· ⚙️ 可配置的替换概率和弹幕属性
· 💬 可选的替换提示信息
· 🔄 支持热重载配置

# 安装方法

1. 将编译后的 TreeAndPotDropReplacer.dll 文件放入 TShock 服务器的 ServerPlugins 文件夹中
2. 重启服务器或使用 /reload 命令加载插件
3. 插件会自动生成配置文件

# 指令

语法 别名 权限 说明
/tadrreload 无 treedrop.reload 重载配置文件
新增指令：
   · /tadrtogglemsg 或 /tadrmsg - 切换消息显示状态
   · 需要权限：treedrop.togglemsg
启用消息：使用 /tadrtogglemsg 或 /tadrmsg 命令
· 禁用消息：再次使用相同命令
· 权限控制：需要 treedrop.togglemsg 权限

配置

配置文件位置: tshock/TreeAndPotDropReplacer.json

```json
{
  "巨石掉落配置": {
    "巨石掉落概率": 15.0,
    "巨石弹幕的ID": 99,
    "巨石弹幕的伤害值": 50,
    "巨石弹幕的击退力": 6.0,
    "是否显示提示信息": true
  },
  "自定义掉落物列表": [
    {
      "物品ID": 9,
      "物品数量": 10,
      "掉落概率": 25.0,
      "掉落来源": 3,
      "是否显示提示信息": true
    },
    {
      "物品ID": 29,
      "物品数量": 1,
      "掉落概率": 5.0,
      "掉落来源": 1,
      "是否显示提示信息": true
    },
    {
      "物品ID": 73,
      "物品数量": 1,
      "掉落概率": 2.0,
      "掉落来源": 2,
      "是否显示提示信息": true
    }
  ]
}
```

# 配置项说明

· ReplaceChance: 替换概率 (0-100)，默认 20%
· BoulderProjectileID: 巨石弹幕的物品ID，默认 268
· BoulderDamage: 巨石弹幕的伤害值，默认 80
· BoulderKnockback: 巨石弹幕的击退力，默认 8.0
· ShowMessage: 是否显示替换提示信息，默认 true

# 权限节点
· treedrop.reload - 允许使用 /tadrreload 命令重载配置

# 支持的Tile类型

树木

· 普通树 (Tile ID: 5)
· 棕榈树 (Tile ID: 323)
· 红木树 (Tile ID: 324)

罐子

· 普通罐子 (Tile ID: 28)

# 工作机制

1. 当玩家破坏树木或罐子时，插件会检测该行为
2. 根据配置的替换概率决定是否触发替换
3. 如果触发替换，会在玩家位置生成巨石弹幕
4. 原版掉落物将被阻止生成
5. 可选显示替换提示信息

# 注意事项

· 巨石弹幕会从玩家上方生成并下落
· 弹幕会对玩家和NPC造成伤害，请小心使用
· 建议在PVE服务器中使用，PVP服务器可能会影响游戏平衡
· 可以通过调整概率来控制游戏难度

# 故障排除

# 插件未加载

· 检查DLL文件是否放置在正确的 ServerPlugins 文件夹
· 查看服务器日志确认是否有加载错误

# 配置不生效

· 使用 /tadrreload 命令重载配置
· 检查配置文件格式是否正确
· 确认有足够的权限使用命令

# 弹幕不生成

· 检查巨石弹幕的ID是否正确（默认268）
· 确认替换概率设置合理
· 查看服务器日志是否有错误信息

# 版本信息

v1.0.0

· 初始版本发布
· 基础功能实现
· 当前版本: 1.1.0
· 支持TShock版本: 4.5.0+
· 支持泰拉瑞亚版本: 1.4.3+

v1.0.1
. 自定义物品版本发布
. 可新增更多物品

# 开发者信息

· 作者: 泷白
· 许可证: MIT License

反馈

· 优先发issue -> 共同维护的插件库: https://github.com/UnrealMultiple/TShockPlugin
· 次优先: TShock官方群: 816771079
· 国内社区: trhub.cn, bbstr.net, tr.monika.love
