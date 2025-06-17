using StardewValley;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using System;
using System.Xml.Serialization;

namespace wsf
{
    [XmlType("Mods_WSF_CustomRing")]
    public class CustomRing : Ring
    {
        public static readonly string RingId = "jounalee.WSF/CustomRing";
        private static string ringId = "517"; // 使用游戏内已有的紫水晶戒指ID作为基础
        private static IModHelper? _helper;
        private static IMonitor? _monitor;
        
        // 保存原始外观状态
        private Hat? originalHat;
        private Clothing? originalShirt;
        private Clothing? originalPants;
        private Color originalHairColor;
        
        public CustomRing() : base(ringId)
        {
            // 直接设置基础描述字段
            this.description = _helper?.Translation.Get("customRing.description") ?? "这是一枚特别的戒指，专为口嗨姐制作。";
            // 确保名称也设置
            this.displayName = _helper?.Translation.Get("customRing.name") ?? "专门给王思帆的戒指";
        }

        public override string DisplayName => _helper?.Translation.Get("customRing.name") ?? "专门给王思帆的戒指";

        public override string getDescription()
        {
            // 强制返回自定义描述，不调用基类方法
            return _helper?.Translation.Get("customRing.description") ?? "这是一枚特别的戒指，专为口嗨姐制作。";
        }
        
        protected override void initNetFields()
        {
            base.initNetFields();
            // 确保网络同步时也使用自定义名称
            this.displayName = _helper?.Translation.Get("customRing.name") ?? "专门给王思帆的戒指";
        }

        private int emoteCooldown = 0;
        
        public override void update(GameTime time, GameLocation environment, Farmer who)
        {
            base.update(time, environment, who);
            
            // 持续显示爱心表情
            if (emoteCooldown <= 0)
            {
                who.doEmote(20);  // 爱心表情
                emoteCooldown = 120;  // 每2秒显示一次(60帧/秒)
            }
            else
            {
                emoteCooldown--;
            }
        }

        public override void onUnequip(Farmer who)
        {
            base.onUnequip(who);
            Game1.addHUDMessage(new HUDMessage("摘戒指了嘛你这家伙...", 3));
            // 恢复原始外观
            who.hat.Value = originalHat;
            if (originalShirt != null) {
                who.changeShirt(originalShirt.ItemId);
            }
            if (originalPants != null) {
                who.pantsItem.Value = originalPants;
                who.changeGender(who.IsMale); // 强制刷新外观
            }
            who.hairstyleColor.Value = originalHairColor;
            who.changeGender(who.IsMale);  // 强制刷新角色外观
            
            _monitor?.Log($"{who.Name} 摘下了ljy给wsf做的戒指，虽然他有点sad...", LogLevel.Info);
        }

        public override void onEquip(Farmer who)
        {
            base.onEquip(who);
            Game1.addHUDMessage(new HUDMessage("这可是ljy精心制作的戒指哦！", 1));
            _monitor?.Log($"{who.Name} 佩戴了ljy给wsf做的戒指!", LogLevel.Info);
            who.animateOnce(294); // 播放星之果实动画

            // 保存原始外观
            originalHat = who.hat.Value;
            originalShirt = who.shirtItem.Value;
            originalPants = who.pantsItem.Value;
            originalHairColor = who.hairstyleColor.Value;

            // 换上婚纱
            who.hat.Value = new Hat("69");  // 新娘头饰(69是婚纱头饰ID)
            who.changeShirt("1265");  // 婚纱上衣
            who.pantsItem.Value = new Clothing("2");  // 婚纱下装
            who.changeGender(who.IsMale);  // 强制刷新角色外观
            // who.doEmote(20);  // 显示爱心表情
        }

        public static void Register(IModHelper helper, IMonitor monitor)
        {
            _helper = helper;
            _monitor = monitor;
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("TileSheets/rings"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsImage();
                    try 
                    {
                        // 加载自定义戒指图标
                        if (_helper != null)
                        {
                            var ringTexture = _helper.ModContent.Load<Texture2D>("assets/ring_icon.png");
                            // 缩放1024x1024贴图到16x16区域
                            editor.PatchImage(ringTexture, targetArea: new Rectangle(0, 0, 16, 16), 
                                sourceArea: new Rectangle(0, 0, 1024, 1024));
                            _monitor?.Log("成功加载并应用自定义戒指贴图", LogLevel.Info);
                        }
                    }
                    catch (Exception ex)
                    {
                        _monitor?.Log($"加载戒指贴图失败: {ex.Message}", LogLevel.Error);
                    }
                });
            }
        }
    }
}
