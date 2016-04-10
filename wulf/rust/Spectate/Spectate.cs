ï»¿using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Spectate", "Wulf/lukespragg", "0.1.2", ResourceId = 1426)]
    [Description("Allows only players with permission to spectate.")]

    class Spectate : RustPlugin
    {
        // Do NOT edit this file, instead edit Spectate.json in server/<identity>/oxide/config

        #region Configuration

        // Messages
        string NoPermission => GetConfig("NoPermission", "Sorry, you can't use 'spectate' right now");
        string NoTargets => GetConfig("NoTargets", "No valid spectate targets!");
        string SpectateStart => GetConfig("SpectateStart", "Started spectating");
        string SpectateStop => GetConfig("SpectateStop", "Stopped spectating");

        // Settings
        string ChatCommand => GetConfig("ChatCommand", "spectate");

        protected override void LoadDefaultConfig()
        {
            // Messages
            Config["NoPermission"] = NoPermission;
            Config["NoTargets"] = NoTargets;
            Config["SpectateStart"] = SpectateStart;
            Config["SpectateStop"] = SpectateStop;

            // Settings
            Config["ChatCommand"] = ChatCommand;

            SaveConfig();
        }

        #endregion

        #region General Setup

        void Loaded()
        {
            LoadDefaultConfig();
            permission.RegisterPermission("spectate.allowed", this);
            cmd.AddChatCommand(ChatCommand, this, "SpectateChatCmd");
        }

        #endregion

        #region Chat Command

        void SpectateChatCmd(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "spectate.allowed"))
            {
                SendReply(player, NoPermission);
                return;
            }

            if (!player.IsSpectating())
            {
                var target = (args.Length > 0 ? BasePlayer.Find(args[0])?.displayName : string.Empty);
                if (string.IsNullOrEmpty(target) || target == player.displayName)
                {
                    PrintToChat(player, NoTargets);
                    return;
                }

                // Put player in spectator mode
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.gameObject.SetLayerRecursive(10);
                player.CancelInvoke("MetabolismUpdate");
                player.CancelInvoke("InventoryUpdate");

                // Set spectate target
                player.UpdateSpectateTarget(target);

                // Set player to third-person view
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);

                PrintToChat(player, SpectateStart);
            }
            else
            {
                // Restore player to normal mode/view
                player.SetParent(null, 0);
                player.metabolism.Reset();
                player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * Random.Range(0.99f, 1.01f));
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.gameObject.SetLayerRecursive(17);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);

                PrintToChat(player, SpectateStop);
            }
        }

        #endregion

        #region Command Redirect

        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.cmd?.namefull != "global.spectate" || arg.connection == null) return null;

            var player = arg.connection.player as BasePlayer;
            var target = new[] { arg.GetString(0) };
            SpectateChatCmd(player, null, target);

            return true;
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
