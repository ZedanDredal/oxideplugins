ï»¿using System;

namespace Oxide.Plugins
{
    [Info("ThirdPerson", "Wulf/lukespragg", "0.1.4", ResourceId = 1424)]
    [Description("Allows any player with permission to use third-person view.")]

    class ThirdPerson : RustPlugin
    {
        // Do NOT edit this file, instead edit ThirdPerson.json in server/<identity>/oxide/config

        #region Configuration

        string ChatCommand => GetConfig("ChatCommand", "view");
        string NoPermission => GetConfig("NoPermission", "Sorry, you can't use 'view' right now");

        protected override void LoadDefaultConfig()
        {
            Config["ChatCommand"] = ChatCommand;
            Config["NoPermission"] = NoPermission;

            SaveConfig();
        }

        #endregion

        #region General Setup

        void Loaded()
        {
            LoadDefaultConfig();

            permission.RegisterPermission("thirdperson.allowed", this);
            cmd.AddChatCommand(ChatCommand, this, "ViewChatCmd");
        }

        #endregion

        void OnPlayerInit(BasePlayer player)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
        }

        #region Chat Command

        void ViewChatCmd(BasePlayer player)
        {
            if (!HasPermission(player, "thirdperson.allowed"))
            {
                SendReply(player, NoPermission);
                return;
            }

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, !player.HasPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode));
        }

        #endregion

        #region Helper Methods

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion
    }
}
