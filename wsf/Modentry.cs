using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace wsf;

public class Modentry : Mod
{
    public override void Entry(IModHelper helper)
    {
        Monitor.Log("Initializing Custom Ring Mod", LogLevel.Debug);
        
        // 注册自定义戒指
        CustomRing.Register(helper, this.Monitor);
        
        // 在存档加载时自动给予戒指
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        bool hasRing = false;
        foreach (var item in Game1.player.Items)
        {
            if (item is Ring ring && ring.GetType() == typeof(CustomRing))
            {
                hasRing = true;
                break;
            }
        }
        
        if (!hasRing)
        {
            Game1.player.addItemToInventory(new CustomRing());
            Monitor.Log("Auto-given custom ring to player", LogLevel.Info);
        }
    }
}
