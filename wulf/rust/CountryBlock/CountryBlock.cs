﻿/*
TODO:
- Add support for country, country code, and Steam ID in messages
- Add support for other games once Covalence is supported
- Fix rare RPC errors with Hurtworld when a player gets kicked
- Switch to GeoIP API once plugin issues have been resolved
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("CountryBlock", "Wulf/lukespragg", "1.0.2")]
    [Description("Block or allow players only from configured countries.")]

    class CountryBlock : CovalencePlugin
    {
        // Do NOT edit this file, instead edit CountryBlock.json in oxide/config and CountryBlock.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Configuration

        bool AdminExcluded => GetConfig("AdminExcluded", true);
        List<object> CountryList => GetConfig("CountryList", new List<object> { "CN", "RU" });
        bool Whitelist => GetConfig("Whitelist", false);

        protected override void LoadDefaultConfig()
        {
            Config["AdminExluded"] = AdminExcluded;
            Config["CountryList"] = CountryList;
            Config["Whitelist"] = Whitelist;
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>            {
                {"PlayerRejected", "This server doesn't allow players from {country}"}
            }, this);
        }

        #endregion

        #region Initialization

        void Loaded()
        {
            #if !HURTWORLD && !REIGNOFKINGS && !RUST && !RUSTLEGACY
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission("countryblock.bypass", this);
        }

        #endregion

        #region Country Lookup

        void IsCountryBlocked(string name, string userId, string ip)
        {
            if (HasPermission(userId, "countryblock.bypass") || IsLocalIp(ip)) return;

            var providers = new[]
            {
                $"http://ip-api.com/line/{ip}?fields=countryCode",
                $"http://geoip.nekudo.com/api/{ip}",
                $"http://ipinfo.io/{ip}/country"
            };
            var url = providers[new Random().Next(providers.Length)];
            webrequest.EnqueueGet(url, (code, response) =>
            {
                if (code != 200 || response == null || response == "undefined" || response == "xx")
                {
                    Puts($"Getting country for {ip} failed! ({code})");
                    Puts(response);
                    return;
                }

                string country;
                try
                {
                    var json = JObject.Parse(response);
                    country = (string) json["country"]["code"];
                }
                catch
                {
                    country = Regex.Replace(response, @"\t|\n|\r|\s.*", "");
                }

                if ((!CountryList.Contains(country) && Whitelist) || (CountryList.Contains(country) && !Whitelist))
                {
                    var player = players.FindOnlinePlayer(name);
                    PlayerRejection(player, country);
                }
            }, this);
        }

        #endregion

        #region Player Rejection

        void PlayerRejection(ILivePlayer player, string country)
        {
            player.Kick(GetMessage("PlayerRejected", player.BasePlayer.UniqueID).Replace("{country}", country));
        }

        #if HURTWORLD
        void OnPlayerConnected(PlayerSession session)
        {
            IsCountryBlocked(session.Name, session.SteamId.ToString(), IpAddress(session.Player.ipAddress));
        }
        #endif

        #if REIGNOFKINGS
        void OnPlayerConnected(CodeHatch.Engine.Networking.Player player)
        {
            IsCountryBlocked(player.Name, player.Id.ToString(), IpAddress(player.Connection.IpAddress));
        }
        #endif

        #if RUST
        void OnPlayerConnected(Network.Message packet)
        {
            IsCountryBlocked(packet.connection.username, packet.connection.userid.ToString(), IpAddress(packet.connection.ipaddress));
        }
        #endif

        #if RUSTLEGACY
        void OnPlayerConnected(NetUser netuser)
        {
            IsCountryBlocked(netuser.displayName, netuser.userID.ToString(), IpAddress(netuser.networkPlayer.ipAddress));
        }
        #endif

        #endregion

        #region Helper Methods

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        bool HasPermission(string userId, string perm) => permission.UserHasPermission(userId, perm);

        static bool IsLocalIp(string ipAddress)
        {
            var split = ipAddress.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            var ip = new[] { int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) };
            return ip[0] == 10 || ip[0] == 127 || (ip[0] == 192 && ip[1] == 168) || (ip[0] == 172 && (ip[1] >= 16 && ip[1] <= 31));
        }

        static string IpAddress(string ip)
        {
            #if DEBUG
            return "8.8.8.8"; // US
            #else
            return Regex.Replace(ip, @":{1}[0-9]{1}\d*", "");
            #endif
        }

        #endregion
    }
}
