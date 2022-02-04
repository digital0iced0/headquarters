using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Headquarters", "digital0iced0", "0.0.3")]
    [Description("Allows players to have one protected headquarters base until they're ready to participate in raiding.")]
    public class Headquarters : RustPlugin
    {
        #region Declaration

        private static PluginData _data;

        private static ConfigFile cFile;

        private bool _freeForAllActive = false;

        private Timer _ffaCheckTimer;

        // Permissions
        private const string AdminPermissionName = "headquarters.admin";

        #endregion

        #region Config

        private class ConfigFile
        {
            public HeadquartersConfig HeadquartersConfig;

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    HeadquartersConfig = new HeadquartersConfig()
                    {
                        Radius = 50f,
                        DistanceToTC = 5f,
                        FreeForAllEnabled = true,
                        FreeForAllHoursAfterWipe = 144f,
                        MarkerPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab",
                        ProtectedColor = "#708A43",
                        UnprotectedColor = "#9A4A3B"
                    },

                };
            }
        }

        private class HeadquartersConfig
        {
            public float Radius { get; set; }
            public float DistanceToTC { get; set; }

            public bool FreeForAllEnabled { get; set; }

            public float FreeForAllHoursAfterWipe { get; set; }

            public string MarkerPrefab { get; set; } = "";

            public string ProtectedColor { get; set; } = "";

            public string UnprotectedColor { get; set; } = "";
        }


        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating default configuration file...");
            cFile = ConfigFile.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            cFile = Config.ReadObject<ConfigFile>();
        }

        protected override void SaveConfig() => Config.WriteObject(cFile);

        #endregion


        #region Lang

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Server_Welcome"] = "This server is running the Headquarter mod.  It allows you to protect one of your bases from attack.  For more details type /hq.help in chat",
                ["Headquarter_Protected_NoDamage"] = "This base is under the protection of a headquarter.  It can't be damaged at this time.",
                ["Headquarter_Unprotected_NoDamage"] = "This headquarter is unprotected.  However, you can't attack it because your headquarter is still protected.",
                ["Headquarter_Exists_Cant_Clear_List"] = "A protected headquarter exists at this location.  You must disable its protection before being able to clear its privilege list.",
                ["Headquarter_Exists_Cant_Deauth"] = "A protected headquarter exists at this location.  You must disable its protection before being able to deauthorize from it's Tool Cupboard.",
                ["Headquarter_Inside_Headquarter"] = "You can't create a headquarter inside another headquarter.",
                ["Headquarter_Not_Inside"] = "You're not inside a headquarter.",
                ["Headquarter_Start_Near_TC"] = "You must stand next to your base's Tool Cupboard to start a headquarter.",
                ["Headquarter_Successful_Start"] = "You have started a protected headquarter at this base! You can invite others to join your headquarter by having them authenticate at the Tool Cupboard.  To keep your headquarter base secure, put a lock on your Tool Cupboard.",
                ["Headquarter_Already_Started"] = "You have already started or joined a headquarter.  This disqualifies you from creating a new headquarter, but you can join another player's headquarter.",
                ["Headquarter_Founder_Quit_Promoted"] = "Your previous headquarter had other members, therefore, one of them has been promoted to lead it.  You have also been deauthed from its Tool Cupboard.",
                ["Headquarter_Founder_Quit_Empty"] = "Your previous headquarter has been disbanded.  Since you were its only member you still maintain access to the base.",
                ["Headquarter_Disabled_Protection"] = "You have disabled your headquarter's protection.  You can now raid unprotected headquarters.",
                ["Headquarter_Only_Founder_Disable"] = "Only the founder of a headquarter can disable its protection.",
                ["Headquarter_Found_Count"] = "Found {0} headquarters.",
                ["Headquarter_Near_Found"] = "Headquarter {0} near {1}.",
                ["Headquarter_Already_Member"] = "You're already part of this headquarter.",
                ["Headquarter_Cleared"] = "All headquarters have been removed.  Protections are disabled.",
                ["Headquarter_Here"] = "There is a headquarter at this position.",
                ["Headquarter_Empty_Here"] = "There isn't a headquarter at this position.",

                ["Free_For_All_Active"] = "<color=green>Headquarter free for all is active! Headquarter protections are disabled!</color>",
                ["Free_For_All_Stopped"] = "<color=red>Headquarter free for all is deactivated!</color>",
                ["Free_For_All_Status"] = "Headquarter free for all is expected {0}",
                ["Free_For_All_Only_Admin"] = "Headquarter free for all is deactivated.  Only an admin can enable it.",

                ["Cmd_Permission"] = "You don't have permission to use that command",
                ["Cmd_Remove_Heaquarter_Founder_Missing"] = "Please provide the user id of the player whose headquarter you wish to remove.",
                ["Cmd_Headquarter_Removed"] = "The founder's headquarter has been removed.",
                ["Cmd_Headquarter_Remove_Fail"] = "Could not find a headquarter belonging to this founder.",

                ["Help_Welcome"] = "Welcome to headquarters.  This mod allows you to protect one of your bases.  Keep this in mind as you play the game.  You can only belong to one headquarter at a time.  If you're a member of a headquarter and authenticate at another headquarter's Tool Cupboard you will be removed from the previous headquarter.  Below are the available player commands.",
                ["Help_Start"] = "Starts a protected Headquarter at one of your bases' Tool Cupboard.",
                ["Help_Disable_Protection"] = "(Founder Only) Allows you to disable your headquarter's protection. This will allow you to participate in raiding.",
                ["Help_FFA"] = "Provides details on how long until free for all is activated.",
                ["Help_Check"] = "Lets you know if there is a headquarter at your current position.",

                ["Time_In_Hours"] = "in approximately {0} hours.",
                ["Time_In_Minutes"] = "in approximately {0} minutes.",
                ["Time_Soon"] = "any moment now!",
                ["Protected"] = "protected",
                ["Unprotected"] = "unprotected",
            }, this);
        }

        #endregion

        #region Helper Methods
        string GetGrid(Vector3 pos)
        {
            char letter = 'A';
            var x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            var z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f)) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((int)letter) + x);
            return $"{letter}{z - 1}";
        }

        private Headquarter GetPlayerHeadquarter(BasePlayer player)
        {
            if (IsFounder(player))
            {
                return _data.AvailableHeadquarters[player.UserIDString];
            }
            else if (IsMember(player))
            {
                var founderId = _data.MemberPlayers[player.UserIDString].FounderId;

                if (_data.AvailableHeadquarters.ContainsKey(founderId))
                {
                    return (_data.AvailableHeadquarters[founderId]);
                }
            }

            return null;
        }

        private bool IsFounder(BasePlayer player)
        {
            return _data.AvailableHeadquarters.ContainsKey(player.UserIDString);
        }

        private bool IsMember(BasePlayer player)
        {
            return _data.MemberPlayers.ContainsKey(player.UserIDString);
        }
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _data);

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PluginData();
        }
        #endregion

        #region Helper Classes
        private class PluginData
        {
            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, Headquarter> AvailableHeadquarters = new Dictionary<string, Headquarter>() { };

            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, HeadquarterMember> MemberPlayers = new Dictionary<string, HeadquarterMember>() { };
        }

        private class HeadquarterMember
        {
            public string UserId { get; }

            // This is the id of the founder of the headquarter this player belongs to
            public string FounderId { get; set; }

            public HeadquarterMember(string user, string founder)
            {
                this.UserId = user;
                this.FounderId = founder;
            }
        }

        private class Headquarter
        {
            public string FounderId { get; set; }
            public float PositionX { get; }
            public float PositionY { get; }
            public float PositionZ { get; }
            public bool HasProtection { get; set; }
            public List<string> MemberIds { get; set; } = new List<string>();

            public MapMarkerGenericRadius marker;

            public DateTime DisabledAt { get; set; }

            public Headquarter(string user, float positionX, float positionY, float positionZ)
            {
                this.FounderId = user;
                this.PositionX = positionX;
                this.PositionY = positionY;
                this.PositionZ = positionZ;
                this.HasProtection = true;
                this.CreateMapMarker();
            }

            public bool HasMember(string user)
            {
                return user == this.FounderId || this.MemberIds.Contains(user);
            }

            public Vector3 getPosition()
            {
                return new Vector3(this.PositionX, this.PositionY, this.PositionZ);
            }

            public void CreateMapMarker(bool freeForAllActive = false)
            {
                if (marker != null)
                {
                    return;
                }

                marker = GameManager.server.CreateEntity(cFile.HeadquartersConfig.MarkerPrefab, getPosition()) as MapMarkerGenericRadius;

                if (marker != null)
                {
                    marker.alpha = 0.6f;

                    var configuredProtectedColor = cFile.HeadquartersConfig.ProtectedColor;
                    var configuredUnprotectedColor = cFile.HeadquartersConfig.UnprotectedColor;

                    if (!ColorUtility.TryParseHtmlString((HasProtection && !freeForAllActive) ? configuredProtectedColor : configuredUnprotectedColor, out marker.color1))
                    {
                        marker.color1 = Color.black;
                    }

                    if (!ColorUtility.TryParseHtmlString((HasProtection && !freeForAllActive) ? configuredProtectedColor : configuredUnprotectedColor, out marker.color2))
                    {
                        marker.color2 = Color.white;
                    }

                    marker.radius = 0.2f;
                    marker.enableSaving = false;
                    marker.Spawn();
                    marker.SendUpdate();
                }
            }

            public void RemoveMapMarker()
            {
                if (marker != null)
                {
                    marker.Kill();
                    marker.SendUpdate();
                    marker.SendNetworkUpdate();
                    UnityEngine.Object.Destroy(marker);
                    marker = null;
                }
            }

            public void UpdateMapMarker()
            {
                if (marker != null)
                {
                    marker.SendNetworkUpdate();
                    marker.SendUpdate();
                }
            }

            public void RefreshMapMarker(bool freeForAllActive = false)
            {
                RemoveMapMarker();
                CreateMapMarker(freeForAllActive);
            }
        }
        #endregion

        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(AdminPermissionName, this);
            SaveConfig();
        }

        private void OnServerInitialized(bool initial)
        {
            LoadData();
            LoadMapMarkers();

            if (cFile.HeadquartersConfig.FreeForAllEnabled)
            {
                _ffaCheckTimer = timer.Every(5f, () =>
                {
                    if (cFile.HeadquartersConfig.FreeForAllEnabled && DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime).TotalHours >= cFile.HeadquartersConfig.FreeForAllHoursAfterWipe)
                    {
                        _freeForAllActive = true;
                        _ffaCheckTimer?.Destroy();
                        RefreshMapMarkers();
                        foreach (var player in BasePlayer.activePlayerList)
                        {
                            PrintToChat(player, Lang("Free_For_All_Active", player.UserIDString));
                        }
                    }
                });
            }
        }

        private void OnServerSave()
        {
            RemoveMapMarkers();
            SaveData();
        }

        private void Unload()
        {
            RemoveMapMarkers();
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }

            RefreshMapMarkers();

            SendReply(player, Lang("Server_Welcome", player.UserIDString));
            if (cFile.HeadquartersConfig.FreeForAllEnabled)
            {
                OutputFFAStatus(player);
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {

            if (_freeForAllActive)
            {
                return null;
            }

            if (entity is BuildingBlock || entity.name.Contains("deploy") || entity.name.Contains("building"))
            {
                var attacker = info?.Initiator?.ToPlayer();

                bool isSteamId = entity.OwnerID.IsSteamId();

                bool isTC = (entity is BuildingPrivlidge);

                if (attacker == null || (!isTC && isSteamId))
                {
                    return null;
                }

                bool isAttackerAuthed = attacker.IsBuildingAuthed(entity.transform.position, entity.transform.rotation, entity.bounds);

                if (!isTC && isAttackerAuthed)
                {
                    return null;
                }

                var headquarter = GetHeadquarterAtPosition(entity.transform.position);

                if (headquarter != null)
                {
                    if (headquarter.HasProtection)
                    {
                        // If we're still here, either its a Headquarter's TC, OR its a building block which is protected from damage for this player.
                        SendReply(info.InitiatorPlayer, Lang("Headquarter_Protected_NoDamage", attacker.UserIDString));
                        return true;
                    }
                    else
                    {
                        // If this headquarter is not protected, lets figure out if the user can attack it

                        var playerHeadquarter = GetPlayerHeadquarter(info.InitiatorPlayer);

                        if (playerHeadquarter != null && playerHeadquarter.HasProtection)
                        {
                            // This player is part of a protected HQ so it can't damage other players' HQ
                            SendReply(info.InitiatorPlayer, Lang("Headquarter_Unprotected_NoDamage", attacker.UserIDString));
                            return true;
                        }
                    }
                }
            }

            return null;
        }

        private Headquarter GetHeadquarterAtPosition(Vector3 position)
        {
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                if (Vector3.Distance(position, currentHeadquarter.Value.getPosition()) <= cFile.HeadquartersConfig.Radius)
                {
                    return currentHeadquarter.Value;
                }
            }

            return null;
        }

        object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            var headquarter = GetHeadquarterAtPosition(player.transform.position);

            if (headquarter != null && headquarter.HasProtection)
            {
                SendReply(player, Lang("Headquarter_Exists_Cant_Clear_List", player.UserIDString));
                return true;
            }

            return null;
        }

        object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var headquarter = GetHeadquarterAtPosition(player.transform.position);

            if (headquarter != null && headquarter.HasProtection)
            {
                SendReply(player, Lang("Headquarter_Exists_Cant_Deauth", player.UserIDString));
                return true;
            }

            return null;
        }

        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var potentialHeadquarter = GetHeadquarterAtPosition(player.transform.position);

            if (potentialHeadquarter != null)
            {
                Headquarter nearestHeadquarter = (Headquarter)potentialHeadquarter;

                // If this player is a founder of another headquarter
                if (IsFounder(player))
                {
                    Headquarter founderPreviousHQ = _data.AvailableHeadquarters[player.UserIDString] as Headquarter;

                    if (founderPreviousHQ != null)
                    {
                        // If somehow the founder was removed, and is attempting to create another TC in same area, allow it
                        if (founderPreviousHQ.FounderId == nearestHeadquarter.FounderId)
                        {
                            return null;
                        }

                        founderPreviousHQ.RemoveMapMarker();

                        // If there are any members left, promote one to new founder and build a replacement HQ
                        if (founderPreviousHQ.MemberIds.Any())
                        {
                            string newFounder = (string)(founderPreviousHQ.MemberIds.First());

                            _data.MemberPlayers.Remove(newFounder);

                            var newHQ = new Headquarter(newFounder, founderPreviousHQ.PositionX, founderPreviousHQ.PositionY, founderPreviousHQ.PositionZ);

                            founderPreviousHQ.MemberIds.ForEach(memberId => _data.MemberPlayers[memberId].FounderId = newFounder);

                            _data.AvailableHeadquarters.Add(newFounder, newHQ);

                            DeauthPlayerFromTC(player, privilege);

                            SendReply(player, Lang("Headquarter_Founder_Quit_Promoted", player.UserIDString));
                        }
                        else
                        {
                            SendReply(player, Lang("Headquarter_Founder_Quit_Empty", player.UserIDString));
                        }
                    }

                    // dismantle old headquarter
                    _data.AvailableHeadquarters.Remove(player.UserIDString);
                }

                // If this player is a member of another headquarter
                if (IsMember(player))
                {
                    if (_data.AvailableHeadquarters.ContainsKey(_data.MemberPlayers[player.UserIDString].FounderId))
                    {
                        Headquarter memberPreviousHQ = _data.AvailableHeadquarters[_data.MemberPlayers[player.UserIDString].FounderId] as Headquarter;

                        if (memberPreviousHQ != null)
                        {
                            DeauthPlayerFromTC(player, privilege);

                            //remove membership there
                            memberPreviousHQ.MemberIds.Remove(player.UserIDString);
                        }
                    }
                    _data.MemberPlayers.Remove(player.UserIDString);
                }

                // allow to join
                nearestHeadquarter.MemberIds.Add(player.UserIDString);
                _data.MemberPlayers.Add(player.UserIDString, new HeadquarterMember(player.UserIDString, nearestHeadquarter.FounderId));
            }

            return null;
        }
        #endregion

        #region Actions

        private void OutputFFAStatus(BasePlayer player)
        {
            if (_freeForAllActive)
            {
                SendReply(player, Lang("Free_For_All_Active", player.UserIDString));
            }
            else if (cFile.HeadquartersConfig.FreeForAllEnabled)
            {
                var timeLeft = cFile.HeadquartersConfig.FreeForAllHoursAfterWipe - DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime).TotalHours;
                string outLeft;

                if (timeLeft > 2)
                {
                    outLeft = Lang("Time_In_Hours", player.UserIDString, ((int)timeLeft).ToString());
                }
                else if (timeLeft < .2)
                {
                    outLeft = Lang("Time_Soon", player.UserIDString);
                }
                else
                {
                    outLeft = Lang("Time_In_Minutes", player.UserIDString, ((int)(timeLeft * 60)).ToString());
                }


                SendReply(player, Lang("Free_For_All_Status", player.UserIDString, outLeft));
            }
            else
            {
                SendReply(player, Lang("Free_For_All_Only_Admin", player.UserIDString));
            }
        }

        private void LoadMapMarkers()
        {
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.CreateMapMarker();
            }
        }

        private void RefreshMapMarkers()
        {
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.RefreshMapMarker(_freeForAllActive);
            }
        }

        private void RemoveMapMarkers()
        {
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.RemoveMapMarker();
            }
        }

        private void DeauthPlayerFromTC(BasePlayer player, BuildingPrivlidge privilege)
        {
            var found = privilege.authorizedPlayers.Find(e => e.userid == player.userID);

            if (found != null)
            {
                privilege.authorizedPlayers.Remove(found);
            }
        }
        #endregion

        #region Commands

        [ChatCommand("hq.help")]
        private void cmdChatHeadquarterListAll(BasePlayer player, string command)
        {
            SendReply(player, Lang("Help_Welcome", player.UserIDString));
            SendReply(player, "/hq.start --- " + Lang("Help_Start", player.UserIDString));
            SendReply(player, "/hq.disable-protection --- " + Lang("Help_Disable_Protection", player.UserIDString));
            SendReply(player, "/hq.ffa --- " + Lang("Help_FFA", player.UserIDString));
            SendReply(player, "/hq.check --- " + Lang("Help_Check", player.UserIDString));
        }

        [ChatCommand("hq.start")]
        private void cmdChatHeadquarterStart(BasePlayer player, string command)
        {
            var existingHeadquarterHere = GetHeadquarterAtPosition(player.transform.position);

            if (existingHeadquarterHere != null)
            {
                SendReply(player, Lang("Headquarter_Inside_Headquarter", player.UserIDString));
                return;
            }

            // ensure next to TC
            List<BaseCombatEntity> cblist = new List<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(player.transform.position, cFile.HeadquartersConfig.DistanceToTC, cblist);

            bool nextToCupboard = false;
            BuildingPrivlidge tc = null;

            foreach (BaseCombatEntity bp in cblist)
            {
                if (bp is BuildingPrivlidge)
                {
                    tc = (BuildingPrivlidge)bp;
                    nextToCupboard = true;
                }
            }

            if (!nextToCupboard)
            {
                SendReply(player, Lang("Headquarter_Start_Near_TC", player.UserIDString));
                return;
            }

            var isNotAssociatedWithHeadquarter = (!IsFounder(player) && !IsMember(player));

            // If this user is not associated with a headquarter
            if (isNotAssociatedWithHeadquarter)
            {
                if (tc != null)
                {
                    ((BuildingPrivlidge)tc).authorizedPlayers.Clear();
                    ((BuildingPrivlidge)tc).authorizedPlayers.Add(new ProtoBuf.PlayerNameID { username = player.name, userid = player.userID });

                    _data.AvailableHeadquarters[player.UserIDString] = new Headquarter(player.UserIDString, tc.transform.position.x, tc.transform.position.y, tc.transform.position.z);

                    SendReply(player, Lang("Headquarter_Successful_Start", player.UserIDString));
                }


                return;
            }
            else
            {
                SendReply(player, Lang("Headquarter_Already_Started", player.UserIDString));
            }
        }

        [ChatCommand("hq.disable-protection")]
        private void cmdChatHeadquarterDisableProtection(BasePlayer player, string command)
        {
            if (IsFounder(player))
            {
                _data.AvailableHeadquarters[player.UserIDString].HasProtection = false;
                _data.AvailableHeadquarters[player.UserIDString].DisabledAt = DateTime.UtcNow;
                _data.AvailableHeadquarters[player.UserIDString].RefreshMapMarker();
                SendReply(player, Lang("Headquarter_Disabled_Protection", player.UserIDString));
                return;
            }
            else
            {
                SendReply(player, Lang("Headquarter_Only_Founder_Disable", player.UserIDString));
                return;
            }
        }

        [ChatCommand("hq.list")]
        private void cmdChatHeadquarterList(BasePlayer player, string command)
        {
            SendReply(player, Lang("Headquarter_Found_Count", player.UserIDString, _data.AvailableHeadquarters.Count.ToString()));
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                SendReply(player, Lang("Headquarter_Near_Found", player.UserIDString, ((currentHeadquarter.Value.HasProtection && !_freeForAllActive) ? "(" + Lang("Protected", player.UserIDString) + ")" : "(" + Lang("Unprotected", player.UserIDString) + ")"), GetGrid(currentHeadquarter.Value.getPosition())));
            }

        }

        [ChatCommand("hq.ffa")]
        private void cmdChatHeadquarterFFA(BasePlayer player, string command)
        {
            OutputFFAStatus(player);
        }

        [ChatCommand("hq.check")]
        private void cmdChatHeadquarterCheck(BasePlayer player, string command)
        {
            var existingHeadquarterHere = GetHeadquarterAtPosition(player.transform.position);

            if (existingHeadquarterHere != null)
            {
                SendReply(player, Lang("Headquarter_Here", player.UserIDString));
                return;
            }

            SendReply(player, Lang("Headquarter_Empty_Here", player.UserIDString));
        }

        [ChatCommand("grid")]
        private void cmdChatHeadquarterGrid(BasePlayer player, string command)
        {
            SendReply(player, GetGrid(player.transform.position));
        }

        [ConsoleCommand("hq.clear-all")]
        private void cmdConsoleHeadquarterClearAll(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
            {
                return;
            }

            if (!permission.UserHasPermission(arg.Connection.userid.ToString(), AdminPermissionName))
            {
                arg.ReplyWith(Lang("Cmd_Permission", arg.Connection.userid.ToString()));
                return;
            }

            RemoveMapMarkers();
            _data.AvailableHeadquarters.Clear();
            _data.MemberPlayers.Clear();
            SaveData();
            PrintToChat(Lang("Headquarter_Cleared", arg.Player().UserIDString));
        }

        [ConsoleCommand("hq.start-ffa")]
        private void cmdConsoleStartFFA(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
            {
                return;
            }

            if (!permission.UserHasPermission(arg.Connection.userid.ToString(), AdminPermissionName))
            {
                arg.ReplyWith(Lang("Cmd_Permission", arg.Connection.userid.ToString()));
                return;
            }

            _freeForAllActive = true;

            RefreshMapMarkers();

            foreach (var player in BasePlayer.activePlayerList)
            {
                PrintToChat(player, Lang("Free_For_All_Active", player.UserIDString));
            }
        }

        [ConsoleCommand("hq.stop-ffa")]
        private void cmdConsoleStopFFA(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
            {
                return;
            }

            if (!permission.UserHasPermission(arg.Connection.userid.ToString(), AdminPermissionName))
            {
                arg.ReplyWith(Lang("Cmd_Permission", arg.Connection.userid.ToString()));
                return;
            }

            _freeForAllActive = false;

            RefreshMapMarkers();

            foreach (var player in BasePlayer.activePlayerList)
            {
                PrintToChat(player, Lang("Free_For_All_Stopped", player.UserIDString));
            }
        }

        [ConsoleCommand("hq.remove")]
        private void cmdConsoleRemoveHeadquarter(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
            {
                return;
            }

            if (!permission.UserHasPermission(arg.Connection.userid.ToString(), AdminPermissionName))
            {
                arg.ReplyWith(Lang("Cmd_Permission", arg.Connection.userid.ToString()));
                return;
            }

            if (!arg.HasArgs())
            {
                arg.ReplyWith(Lang("Cmd_Remove_Heaquarter_Founder_Missing", arg.Connection.userid.ToString()));
            }

            var founderId = arg.Args[0];

            if (_data.AvailableHeadquarters.ContainsKey(founderId))
            {
                var headquarterToRemove = _data.AvailableHeadquarters[founderId];
                headquarterToRemove.RemoveMapMarker();
                headquarterToRemove.MemberIds.ForEach(memberId => _data.MemberPlayers.Remove(memberId));
                _data.AvailableHeadquarters.Remove(founderId);
                arg.ReplyWith(Lang("Cmd_Headquarter_Removed", arg.Connection.userid.ToString()));
            }
            else
            {
                arg.ReplyWith(Lang("Cmd_Headquarter_Remove_Fail", arg.Connection.userid.ToString()));
            }
        }
        #endregion
    }
}
