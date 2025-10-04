using System;
using System.ComponentModel;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TreeAndPotDropReplacer
{
    [ApiVersion(2, 1)]
    public class TreeAndPotDropReplacer : TerrariaPlugin
    {
        public override string Name => "TreeAndPotDropReplacer";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "泷白";
        public override string Description => "替换摇树和砸罐子的掉落物";

        private static string configPath;
        public static Config Config { get; set; }
        private Random random = new Random();

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
            
            // 使用TileEdit事件
            GetDataHandlers.TileEdit += OnTileEdit;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GetDataHandlers.TileEdit -= OnTileEdit;
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
                }
                else
                {
                    Config = new Config();
                    Config.Write(configPath);
                }
                TShock.Log.ConsoleInfo("[TreeAndPotDropReplacer] 配置加载成功!");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[TreeAndPotDropReplacer] 加载配置时出错: {ex}");
                Config = new Config();
            }
        }

        private void ReloadConfig(CommandArgs args)
        {
            LoadConfig();
            args.Player.SendSuccessMessage("[TreeAndPotDropReplacer] 配置已重新加载！");
        }

        private void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args)
        {
            try
            {
                TSPlayer player = args.Player;
                if (player == null || !player.Active || player.TPlayer == null)
                    return;

                // 检查是否是破坏行为（编辑类型为0表示破坏）
                if (args.EditData != 0)
                    return;

                int tileX = args.X;
                int tileY = args.Y;
                
                // 检查坐标是否有效
                if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                    return;

                // 修复错误1: 直接使用Main.tile而不是转换为Tile
                ITile tile = Main.tile[tileX, tileY];
                if (tile == null || !tile.active())
                    return;

                // 检查是否是树或罐子
                bool isTree = IsTreeTile(tile.type);
                bool isPot = tile.type == 28; // 罐子Tile ID

                if (!isTree && !isPot)
                    return;

                // 根据概率决定是否替换掉落物
                if (random.NextDouble() * 100 < Config.ReplaceChance)
                {
                    // 生成巨石弹幕
                    SpawnBoulderProjectile(player);

                    // 发送提示信息
                    if (Config.ShowMessage)
                    {
                        string itemName = isTree ? "树" : "罐子";
                        player.SendWarningMessage($"[TreeAndPotDropReplacer] {itemName}掉落物已被替换为巨石弹幕！");
                    }

                    // 阻止原版掉落
                    args.Handled = true;
                    
                    TShock.Log.ConsoleDebug($"[TreeAndPotDropReplacer] 为玩家 {player.Name} 在位置 ({tileX}, {tileY}) 生成了巨石弹幕");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[TreeAndPotDropReplacer] 处理Tile编辑时出错: {ex}");
            }
        }

        private bool IsTreeTile(ushort tileType)
        {
            // 普通树、棕榈树、红木树等
            return tileType == 5 || tileType == 323 || tileType == 324;
        }

        private void SpawnBoulderProjectile(TSPlayer player)
        {
            try
            {
                int projectileType = Config.BoulderProjectileID;
                int damage = Config.BoulderDamage;
                float knockback = Config.BoulderKnockback;
                
                // 修复错误2: 使用正确的Projectile源创建方法
                // 使用更兼容的Projectile.NewProjectile重载
                int projectileIndex = Projectile.NewProjectile(
                    Projectile.GetNoneSource(), // 使用静态方法获取projectile源
                    player.TPlayer.position.X,
                    player.TPlayer.position.Y - 32f,
                    0f,
                    0f,
                    projectileType,
                    damage,
                    knockback,
                    player.Index
                );

                // 同步弹幕给所有玩家
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projectileIndex);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[TreeAndPotDropReplacer] 生成巨石弹幕时出错: {ex}");
            }
        }
    }

    public class Config
    {
        [Description("替换概率 (0-100)")]
        public float ReplaceChance { get; set; } = 20f;

        [Description("巨石弹幕的ID")]
        public int BoulderProjectileID { get; set; } = 268;

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
                TShock.Log.ConsoleInfo("[TreeAndPotDropReplacer] 配置文件不存在，创建默认配置。");
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