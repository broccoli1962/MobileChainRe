// Auto Generate Code.
using System.Collections.Generic;

namespace Backend.AddressableKey
{
    public static class AddressableKeys
    {
        public static class UI
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class InGame
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "Panel", "InGame/Panel.prefab" },
                { "boom", "Images/4panel/boom.aseprite" },
                { "crash_count", "Images/4panel/crash_count.aseprite" },
                { "empty_panel_fire", "Images/4panel/empty_panel_fire.aseprite" },
                { "empty_panel_grass", "Images/4panel/empty_panel_grass.aseprite" },
                { "empty_panel_light", "Images/4panel/empty_panel_light.aseprite" },
                { "empty_panel_water", "Images/4panel/empty_panel_water.aseprite" },
                { "large_empty_panel_grass", "Images/4panel/large_empty_panel_grass.aseprite" },
                { "large_empty_panel_light", "Images/4panel/large_empty_panel_light.aseprite" },
                { "large_empty_panel_red", "Images/4panel/large_empty_panel_red.aseprite" },
                { "large_empty_panel_water", "Images/4panel/large_empty_panel_water.aseprite" },
                { "count", "Images/GameUI/count.aseprite" },
                { "count_base", "Images/GameUI/count_base.aseprite" },
                { "count_temp", "Images/GameUI/count_temp.aseprite" },
                { "empty_healthbar", "Images/GameUI/empty_healthbar.aseprite" },
                { "EnemyBullet", "Images/GameUI/EnemyBullet.aseprite" },
                { "log_button", "Images/GameUI/log_button.aseprite" },
                { "max_healthbar_1", "Images/GameUI/max_healthbar 1.aseprite" },
                { "monster_turn", "Images/GameUI/monster_turn.aseprite" },
                { "return_button", "Images/GameUI/return_button.aseprite" },
                { "screen", "Images/GameUI/screen.aseprite" },
                { "Target", "Images/GameUI/Target.aseprite" },
                { "TestPlayer", "Images/GameUI/TestPlayer.aseprite" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
