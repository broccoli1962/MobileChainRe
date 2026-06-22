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
                { "BottomNavBar", "UI/BottomNavBar.prefab" },
                { "SegmentedGaugeBar", "UI/Common/SegmentedGaugeBar.prefab" },
                { "SingleGaugeBar", "UI/Common/SingleGaugeBar.prefab" },
                { "DifficultyButton", "UI/DifficultyButton.prefab" },
                { "Icon", "UI/Icon.prefab" },
                { "InGameTopHud", "UI/InGameTopHud.prefab" },
                { "LobbyPanel", "UI/LobbyPanel.prefab" },
                { "Monster", "UI/Monster.prefab" },
                { "OptionPopup", "UI/OptionPopup.prefab" },
                { "QuestBox", "UI/QuestBox.prefab" },
                { "QuestDetailPanel", "UI/QuestDetailPanel.prefab" },
                { "TopNavBar", "UI/TopNavBar.prefab" },
                { "UIBlocker", "UI/UIBlocker.prefab" },
                { "UIRoot", "UI/UIRoot.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class InGame
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioSource", "InGame/AudioSource.prefab" },
                { "CharacterSlot", "InGame/CharacterSlot.prefab" },
                { "Line", "InGame/Line.prefab" },
                { "Panel", "InGame/Panel.prefab" },
                { "PuzzleController", "InGame/PuzzleController.prefab" },
                { "GameScene", "Scenes/GameScene.unity" },
                { "LobbyScene", "Scenes/LobbyScene.unity" },
                { "CirclePanel", "Images/4panel/CirclePanel.png" },
                { "Square", "Images/4panel/Square.png" },
                { "boom", "Images/4panel_old/boom.aseprite" },
                { "crash_count", "Images/4panel_old/crash_count.aseprite" },
                { "empty_panel_fire", "Images/4panel_old/empty_panel_fire.aseprite" },
                { "empty_panel_grass", "Images/4panel_old/empty_panel_grass.aseprite" },
                { "empty_panel_light", "Images/4panel_old/empty_panel_light.aseprite" },
                { "empty_panel_water", "Images/4panel_old/empty_panel_water.aseprite" },
                { "large_empty_panel_grass", "Images/4panel_old/large_empty_panel_grass.aseprite" },
                { "large_empty_panel_light", "Images/4panel_old/large_empty_panel_light.aseprite" },
                { "large_empty_panel_red", "Images/4panel_old/large_empty_panel_red.aseprite" },
                { "large_empty_panel_water", "Images/4panel_old/large_empty_panel_water.aseprite" },
                { "panel0", "Images/4panel_old/panel0.aseprite" },
                { "panel1", "Images/4panel_old/panel1.aseprite" },
                { "panel2", "Images/4panel_old/panel2.aseprite" },
                { "panel3", "Images/4panel_old/panel3.aseprite" },
                { "panel4", "Images/4panel_old/panel4.aseprite" },
                { "CharacterSlotOutLine", "Images/GameUI/Borders/CharacterSlotOutLine.png" },
                { "UIPanelIcon", "Images/GameUI/Borders/UIPanelIcon.png" },
                { "UIPanelIconFillMask", "Images/GameUI/Borders/UIPanelIconFillMask.png" },
                { "UISquareLine", "Images/GameUI/Borders/UISquareLine.png" },
                { "UISquareLineMask", "Images/GameUI/Borders/UISquareLineMask.png" },
                { "count", "Images/GameUI/count.aseprite" },
                { "count_base", "Images/GameUI/count_base.aseprite" },
                { "count_temp", "Images/GameUI/count_temp.aseprite" },
                { "empty_healthbar", "Images/GameUI/empty_healthbar.aseprite" },
                { "EnemyBullet", "Images/GameUI/EnemyBullet.aseprite" },
                { "Floor", "Images/GameUI/Floor.png" },
                { "Btn_Album", "Images/GameUI/Icon/Btn_Album.png" },
                { "Btn_Back", "Images/GameUI/Icon/Btn_Back.png" },
                { "Btn_Home", "Images/GameUI/Icon/Btn_Home.png" },
                { "Btn_Mission", "Images/GameUI/Icon/Btn_Mission.png" },
                { "Btn_Pause", "Images/GameUI/Icon/Btn_Pause.png" },
                { "Btn_Play", "Images/GameUI/Icon/Btn_Play.png" },
                { "Btn_Plus", "Images/GameUI/Icon/Btn_Plus.png" },
                { "Btn_Shop", "Images/GameUI/Icon/Btn_Shop.png" },
                { "Btn_X", "Images/GameUI/Icon/Btn_X.png" },
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

        public static class Sounds
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioMixer", "Sounds/AudioMixer.mixer" },
                { "popSound", "Sounds/popSound.mp3" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
