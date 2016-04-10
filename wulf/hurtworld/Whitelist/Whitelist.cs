﻿using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Whitelist", "Wulf/lukespragg", "1.0.1")]
    [Description("Restricts server access to whitelisted players only.")]

    class Whitelist : CovalencePlugin
    {
        // Do NOT edit this file, instead edit Whitelist.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Localization

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string> { {"NotWhitelisted", "You are not whitelisted"} };
            lang.RegisterMessages(messages, this);
        }

        #endregion

        #region Initialization

        void Loaded()
        {
            #if !HURTWORLD && !REIGNOFKINGS && !RUST && !RUSTLEGACY
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultMessages();
            permission.RegisterPermission("whitelist.allowed", this);
        }

        #endregion

        #region Whitelist Check

        bool IsWhitelisted(string userId) => HasPermission(userId, "whitelist.allowed");

        #if HURTWORLD
        object CanClientLogin(PlayerSession session)
        {
            return !IsWhitelisted(session.SteamId.ToString()) ? GetMessage("NotWhitelisted", session.SteamId.ToString()) : null;
        }
        #endif

        #if REIGNOFKINGS
        object OnUserApprove(CodeHatch.Engine.Core.Networking.ConnectionLoginData data)
        {
            return !IsWhitelisted(data.PlayerId.ToString()) ? (object)ConnectionError.NotInWhitelist : null;
        }
        #endif

        #if RUST
        object CanClientLogin(Network.Connection connection)
        {
            return !IsWhitelisted(connection.userid.ToString()) ? GetMessage("NotWhitelisted", connection.userid.ToString()) : null;
        }
        #endif

        #if RUSTLEGACY
        object CanClientLogin(ClientConnection connection)
        {
            return !IsWhitelisted(connection.UserID.ToString()) ? (object)uLink.NetworkConnectionError.ApprovalDenied : null;
        }
        #endif

        #endregion

        #region Helper Methods

        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        bool HasPermission(string steamId, string perm) => permission.UserHasPermission(steamId, perm);

        #endregion
    }
}
