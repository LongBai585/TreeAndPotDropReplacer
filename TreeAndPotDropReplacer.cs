using System;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace TreeAndPotDropReplacer
{
    [ApiVersion(2, 1)]
    public class TreeAndPotDropReplacer : TerrariaPlugin
    {
        public override string Name => "TreeAndPotDropReplacer";
        public override Version Version => new Version(1, 1, 0);
        public override string Author => "泷白";
        public override string Description => "额外的掉落物";

        private static string configPath;
        public static Config Config { get; set; }
        private Random random = new Random();
        
        // 树叶瓦片类型
        private readonly HashSet<ushort> _leafTiles = new HashSet<ushort> { 384, 385 };

        public TreeAndPotDropReplacer(Main game) : base(game)
        {
            Order = 1;
        }

        public override void Initialize()
        {
            configPath = Path.Combine(TShock.SavePath, "TreeAndPotDropReplacer.json");
            LoadConfig();
            
            // 注册命令
            Commands.ChatCommands.Add(new Command("treepotreplacer.reload", ReloadConfig, "tadrreload"));
            
            // 输出插件信息到控制台
            TShock.Log.ConsoleInfo($"=== {Name} v{Version} 已加载 ===");
            TShock.Log.ConsoleInfo($"作者: {Author}");
            TShock.Log.ConsoleInfo($"描述: {Description}");
            TShock.Log.ConsoleInfo("==============================");
            
            // 直接应用Hook
            try
            {
                // Hook世界生成方法
                On.Terraria.WorldGen.ShakeTree += OnShakeTree;
                On.Terraria.WorldGen.KillTile += OnKillTile;
                
                TShock.Log.ConsoleInfo($"[{Name}] Hook已成功应用！");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{Name}] 应用Hook时出错: {ex}");
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 移除Hook
                On.Terraria.WorldGen.ShakeTree -= OnShakeTree;
                On.Terraria.WorldGen.KillTile -= OnKillTile;
                
                TShock.Log.ConsoleInfo($"[{Name}] 插件已卸载！");
            }
            base.Dispose(disposing);
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    Config = Config.Read(configPath);
                    TShock.Log.ConsoleInfo($"[{Name}] 配置加载成功!");
                }
                else
                {
                    Config = new Config();
                    Config.Write(configPath);
                    TShock.Log.ConsoleInfo($"[{Name}] 默认配置文件已创建!");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{Name}] 加载配置时出错: {ex}");
                Config = new Config();
            }
        }

        private void ReloadConfig(CommandArgs args)
        {
            LoadConfig();
            args.Player.SendSuccessMessage($"[{Name}] 配置已重新加载！");
        }

        // 摇树处理方法 - 修改为从树叶上方掉落
        private void OnShakeTree(On.Terraria.WorldGen.orig_ShakeTree orig, int x, int y)
        {
            // 先调用原版方法
            orig(x, y);
            
            // 检查概率
            if (random.NextDouble() * 100 < Config.ReplaceChance)
            {
                try
                {
                    // 查找树叶位置
                    int leafY = FindLeafTop(x, y);
                    
                    if (leafY > 0)
                    {
                        // 从树叶上方生成巨石
                        SpawnBoulderFromLeafTop(x, leafY);
                        
                        if (Config.ShowMessage)
                        {
                            // 向附近玩家发送消息
                            foreach (TSPlayer player in TShock.Players)
                            {
                                if (player != null && player.Active && 
                                    Math.Abs(player.TileX - x) < 50 && Math.Abs(player.TileY - y) < 50)
                                {
                                    player.SendWarningMessage($"[{Name}] 摇树掉落物有概率触发额外掉落！");
                                }
                            }
                        }
                        
                        TShock.Log.ConsoleDebug($"[{Name}] 在摇树位置 ({x}, {y}) 从树叶上方生成了巨石弹幕");
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"[{Name}] 处理摇树时出错: {ex}");
                }
            }
        }

        // 使用KillTile来检测罐子破坏
        private void OnKillTile(On.Terraria.WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
        {
            ITile tile = Main.tile[i, j];
            bool wasPot = tile.active() && tile.type == TileID.Pots;
            
            // 先调用原版方法
            orig(i, j, fail, effectOnly, noItem);
            
            // 检查是否是罐子并且被成功破坏
            if (wasPot && !fail && !effectOnly)
            {
                // 检查概率
                if (random.NextDouble() * 100 < Config.ReplaceChance)
                {
                    try
                    {
                        // 生成巨石弹幕
                        SpawnBoulderProjectile(i, j, false);
                        
                        if (Config.ShowMessage)
                        {
                            // 向附近玩家发送消息
                            foreach (TSPlayer player in TShock.Players)
                            {
                                if (player != null && player.Active && 
                                    Math.Abs(player.TileX - i) < 50 && Math.Abs(player.TileY - j) < 50)
                                {
                                    player.SendWarningMessage($"[{Name}] 砸罐子掉落物有概率触发额外掉落！");
                                }
                            }
                        }
                        
                        TShock.Log.ConsoleDebug($"[{Name}] 在砸罐子位置 ({i}, {j}) 生成了巨石弹幕");
                    }
                    catch (Exception ex)
                    {
                        TShock.Log.ConsoleError($"[{Name}] 处理砸罐子时出错: {ex}");
                    }
                }
            }
        }

        // 查找树叶顶部位置
        private int FindLeafTop(int x, int startY)
        {
            int currentY = startY;
            
            // 向上查找树叶
            while (currentY > 10)
            {
                ITile tile = Main.tile[x, currentY];
                if (tile.active() && _leafTiles.Contains(tile.type))
                {
                    // 找到树叶，继续向上查找可能的更高树叶
                    while (currentY > 10 && Main.tile[x, currentY - 1].active() && _leafTiles.Contains(Main.tile[x, currentY - 1].type))
                    {
                        currentY--;
                    }
                    return currentY;
                }
                currentY--;
            }
            
            // 如果没有找到树叶，返回原始位置上方
            return startY - 3;
        }

        // 从树叶上方生成巨石
        private void SpawnBoulderFromLeafTop(int x, int leafY)
        {
            try
            {
                int projectileType = Config.BoulderProjectileID;
                int damage = Config.BoulderDamage;
                float knockback = Config.BoulderKnockback;
                
                // 在树叶上方生成巨石
                float worldX = x * 16f + 8f + (float)(random.NextDouble() - 0.5) * 16f;
                float worldY = leafY * 16f - 40f; // 在树叶上方40像素处生成
                
                // 设置向下掉落的速度
                float velocityX = (float)(random.NextDouble() - 0.5) * 2f;
                float velocityY = 1f + (float)random.NextDouble() * 2f; // 向下速度
                
                // 使用兼容的Projectile源
                var projectileSource = Projectile.GetNoneSource();
                
                // 生成弹幕
                int projectileIndex = Projectile.NewProjectile(
                    projectileSource,
                    worldX,
                    worldY,
                    velocityX,
                    velocityY,
                    projectileType,
                    damage,
                    knockback,
                    Main.myPlayer
                );

                // 同步弹幕给所有玩家
                if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles && 
                    Main.projectile[projectileIndex] != null && Main.projectile[projectileIndex].active)
                {
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projectileIndex);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{Name}] 从树叶生成巨石弹幕时出错: {ex}");
            }
        }

        // 罐子的巨石生成方法保持不变
        private void SpawnBoulderProjectile(int tileX, int tileY, bool isTree)
        {
            try
            {
                int projectileType = Config.BoulderProjectileID;
                int damage = Config.BoulderDamage;
                float knockback = Config.BoulderKnockback;
                
                // 将Tile坐标转换为世界坐标
                float worldX = tileX * 16f + 8f;
                float worldY = tileY * 16f;
                
                // 在罐子上方生成
                worldY -= 24f;
                
                // 添加随机速度
                float velocityX = (float)(random.NextDouble() - 0.5) * 4f;
                float velocityY = -2f - (float)random.NextDouble() * 2f;
                
                // 使用兼容的Projectile源
                var projectileSource = Projectile.GetNoneSource();
                
                // 生成弹幕
                int projectileIndex = Projectile.NewProjectile(
                    projectileSource,
                    worldX,
                    worldY,
                    velocityX,
                    velocityY,
                    projectileType,
                    damage,
                    knockback,
                    Main.myPlayer
                );

                // 同步弹幕给所有玩家
                if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles && 
                    Main.projectile[projectileIndex] != null && Main.projectile[projectileIndex].active)
                {
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projectileIndex);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{Name}] 生成巨石弹幕时出错: {ex}");
            }
        }
    }

    public class Config
    {
        [Description("替换概率 (0-100)")]
        public float ReplaceChance { get; set; } = 20f;

        [Description("巨石弹幕的ID")]
        public int BoulderProjectileID { get; set; } = 99;

        [Description("巨石弹幕的伤害值")]
        public int BoulderDamage { get; set; } = 80;

        [Description("巨石弹幕的击退力")]
        public float BoulderKnockback { get; set; } = 8f;

        [Description("是否显示替换提示信息")]
        public bool ShowMessage { get; set; } = true;

        public Config Read(string path)
        {
            if (!File.Exists(path))
            {
                return new Config();
            }
            
            try
            {
                string json = File.ReadAllText(path);
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);
                return config ?? new Config();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[TreeAndPotDropReplacer] 读取配置文件时出错: {ex}");
                return new Config();
            }
        }

        public void Write(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                    
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[TreeAndPotDropReplacer] 写入配置文件时出错: {ex}");
            }
        }
    }
}