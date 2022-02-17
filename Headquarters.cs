using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Headquarters", "digital0iced0", "0.1.2")]
    [Description("Allows players to have one protected headquarters base until they're ready to participate in raiding.")]
    public class Headquarters : RustPlugin
    {
        #region Declaration

        private static PluginData _data;

        private static ConfigFile _config;

        private bool _freeForAllActive = false;

        private Timer _ffaCheckTimer;

        // Permissions
        private const string AdminPermissionName = "headquarters.admin";

        private static readonly string[] StorageTypesPenalizeModules = {
            "2module_camper",
            "1module_storage",
        };

        private static readonly string[] StorageTypes = {
            "skull_fire_pit",
            "bbq.deployed",
            "dropbox.deployed",
            "stocking_small_deployed",
            "campfire",
            "furnace.large",
            "furnace",
            "box.wooden.large",
            "small_stash_deployed",
            "refinery_small_deployed",
            "cupboard.tool.deployed",
            "vendingmachine.deployed",
            "woodbox_deployed",
            "locker.deployed",
        };

        #endregion

        #region Config

        private class ConfigFile
        {
            public HeadquartersConfig HeadquartersConfig;

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    HeadquartersConfig = new HeadquartersConfig(),
                };
            }
        }

        private class HeadquartersConfig
        {
            public float Radius { get; set; } = 27.5f;

            public float DistanceToTC { get; set; } = 2f;

            public bool FreeForAllEnabled { get; set; } = true;

            public float FreeForAllHoursAfterWipe { get; set; } = 144f;

            public string MarkerPrefab { get; set; } = "assets/prefabs/tools/map/genericradiusmarker.prefab";

            public float ProtectionPercent { get; set; } = 100f;

            public float ProtectionPercentMinimum { get; set; } = 10f;

            public float ProtectionSlotsWithoutPenalty { get; set; } = 30f;

            public float ProtectionPenaltyPercentPerSlot { get; set; } = 1.5f;
        }


        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating default configuration file...");
            _config = ConfigFile.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigFile>();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion


        #region Lang

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Server_Welcome"] = "This server is running Headquarters mod.  It allows you to provide added defense to one of your bases.  For more details type /hq.help in chat",
                ["Headquarter_Protected_NoDamage"] = "This base is under the protection of a HQ.  It can't be damaged at this time.",
                ["Headquarter_Unprotected_NoDamage"] = "This HQ is unprotected.  However, you can't attack it because your HQ is still protected.",
                ["Headquarter_Exists_Cant_Clear_List"] = "A protected HQ exists at this location.  You must disable its protection before being able to clear its privilege list.",
                ["Headquarter_Exists_Cant_Deauth"] = "A protected HQ exists at this location.  You must disable its protection before being able to deauthorize from it's Tool Cupboard.",
                ["Headquarter_Inside_Headquarter"] = "You can't create a HQ inside another HQ.",
                ["Headquarter_Not_Inside"] = "You're not inside a HQ.",
                ["Headquarter_Start_Near_TC"] = "You must stand next to your base's Tool Cupboard to start a HQ.",
                ["Headquarter_Successful_Start"] = "You have started a protected HQ at this base! You can invite others to join your HQ by having them authenticate at the Tool Cupboard.  To keep your HQ base secure, put a lock on your Tool Cupboard.",
                ["Headquarter_Already_Started"] = "You have already started or joined a HQ.  This disqualifies you from creating a new HQ, but you can join another player's HQ by authenticating at their Tool Cupboard.",
                ["Headquarter_Founder_Quit_Promoted"] = "Your previous HQ had other members, therefore, one of them has been promoted to lead it.  You have also been deauthed from its Tool Cupboard.",
                ["Headquarter_Founder_Quit_Empty"] = "Your previous HQ has been disbanded.  Since you were its only member you still maintain access to the base.",
                ["Headquarter_Disabled_Protection"] = "You have disabled your HQ's protection.  You can now raid unprotected HQs.",
                ["Headquarter_Only_Founder_Disable"] = "Only the founder of a HQ can disable its protection.",
                ["Headquarter_Found_Count"] = "Found {0} HQs.",
                ["Headquarter_Near_Found"] = "Headquarter {0} near {1}.",
                ["Headquarter_Already_Member"] = "You're already part of this HQ.",
                ["Headquarter_Cleared"] = "All HQs have been removed.  Protections are disabled.",
                ["Headquarter_Here_Protection_Rating"] = "You're in {0}'s HQ! ({1}%).",
                ["Headquarter_Empty_Here"] = "There isn't a HQ at this position.",
                ["Headquarter_Require_Name"] = "You must provide a name for your HQ.",
                ["Headquarter_Deployable_Blocked"] = "You can't deploy this item inside someone else's HQ.",

                ["Free_For_All_Active"] = "<color=green>HQ free for all is active! HQ protections are disabled!</color>",
                ["Free_For_All_Stopped"] = "<color=red>HQ free for all is deactivated!</color>",
                ["Free_For_All_Status"] = "HQ free for all is expected {0}",
                ["Free_For_All_Only_Admin"] = "HQ free for all is deactivated.  Only an admin can enable it.",

                ["Cmd_Permission"] = "You don't have permission to use that command",
                ["Cmd_Remove_Heaquarter_Founder_Missing"] = "Please provide the user id of the player whose HQ you wish to remove.",
                ["Cmd_Headquarter_Removed"] = "The founder's HQ has been removed.",
                ["Cmd_Headquarter_Remove_Fail"] = "Could not find a HQ belonging to this founder.",

                ["Help_Welcome"] = "Welcome to Headquarters! This mod allows you to provide protection for one of your bases by designating it your headquarter (HQ).",
                ["Help_Details"] = "A few simple things to keep in mind: Protection is applied to your HQ building blocks, Tool Cupboard (TC), and doors only (it does not protect windows or other deployables).  You can only belong to one HQ at any given time. You can switch HQ by authenticating at someone else's TC but you will lose your previous HQ.  If you place too many items inside your HQ it will reduce its protection level.  Removing items from the HQ will increase its protection again.  Vehicles with storage capabilities left inside a HQ base will eventually suffer random inventory losses as they decay.",
                ["Help_Raid"] = "You can raid other protected HQ bases but you should check the map to see if they have a high level of protection before attempting to do so (since it may not be worth it).  Non HQ bases can be raided by everyone.  Unprotected HQs (disabled by their founder) can't be damaged by members of a protected HQ.  Their members can damage your protected HQ base though, so ensure you keep a high protection level.",
                ["Help_Start"] = "Starts a named protected Headquarter at one of your bases' Tool Cupboard.",
                ["Help_Start_Name"] = "(name)",
                ["Help_Disable_Protection"] = "(Founder Only) Allows you to disable your HQ's protection. This will allow you to participate in unprotected HQ raiding.",
                ["Help_FFA"] = "Provides details on how long until free for all is activated.",
                ["Help_Check"] = "Lets you know if there is a HQ (and its protection level) at your position.",

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
            return $"{letter}{z}";
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
            public string Name { get; set; }
            public int StorageSlots { get; set; }
            public float PositionX { get; }
            public float PositionY { get; }
            public float PositionZ { get; }
            public bool HasProtection { get; set; }
            public List<string> MemberIds { get; set; } = new List<string>();
            [JsonIgnore]
            public MapMarkerGenericRadius marker;
            public DateTime DisabledAt { get; set; }
            public float LastKnownProtectionPercent { get; set; } = 1;
            public DateTime LastMarkerRefresh { get; set; }

            public Headquarter(string user, string name, float positionX, float positionY, float positionZ, int storageSlots = 0)
            {
                this.FounderId = user;
                this.Name = name;
                this.PositionX = positionX;
                this.PositionY = positionY;
                this.PositionZ = positionZ;
                this.HasProtection = true;
                this.StorageSlots = storageSlots;
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

                marker = GameManager.server.CreateEntity(_config.HeadquartersConfig.MarkerPrefab, getPosition()) as MapMarkerGenericRadius;

                if (marker != null)
                {
                    marker.alpha = 0.6f;
                    marker.name = this.Name;

                    marker.color1 = Color.yellow;
                    marker.color2 = (HasProtection && !freeForAllActive) ? getProtectionColor() : Color.red;

                    marker.radius = 0.2f;
                    marker.Spawn();
                    marker.SendUpdate();
                }
            }

            private Color getProtectionColor()
            {
                if (LastKnownProtectionPercent > .8)
                {
                    return Color.green;
                }
                else if (LastKnownProtectionPercent > .55)
                {
                    return Color.yellow;
                }
                else if (LastKnownProtectionPercent > .3)
                {
                    return new Color(1f, .65f, 0f, 1f); // Orange
                }
                else
                {
                    return Color.red;
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
                if (LastMarkerRefresh == null || DateTime.UtcNow.Subtract(LastMarkerRefresh).TotalSeconds > 5)
                {
                    RemoveMapMarker();
                    CreateMapMarker(freeForAllActive);
                    this.LastMarkerRefresh = DateTime.UtcNow;
                }
            }

            public void RecalculateProtectionScale(float cProtectionPercent, float cProtectionPercentMinimum, float cProtectionSlotsWithoutPenalty, float cProtectionPentaltyPercentPerSlot)
            {
                LastKnownProtectionPercent = Mathf.Min(100, Mathf.Max((cProtectionPercent - ((this.StorageSlots - cProtectionSlotsWithoutPenalty) * cProtectionPentaltyPercentPerSlot)), cProtectionPercentMinimum)) / 100;
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

            if (_config.HeadquartersConfig.FreeForAllEnabled)
            {
                _ffaCheckTimer = timer.Every(15f, () =>
                {
                    if (_config.HeadquartersConfig.FreeForAllEnabled && DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime).TotalHours >= _config.HeadquartersConfig.FreeForAllHoursAfterWipe)
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

            RefreshStorageCounts();

            LoadMapMarkers();
        }

        private void OnServerSave()
        {
            SaveData();
            RefreshMapMarkers();
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
            if (_config.HeadquartersConfig.FreeForAllEnabled)
            {
                OutputFFAStatus(player);
            }
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null)
            {
                return;
            }

            var entity = go.ToBaseEntity();
            var player = plan.GetOwnerPlayer();

            string prefabName = entity?.ShortPrefabName ?? "unknown";

            if (entity == null || player == null || !StorageTypes.Contains(prefabName))
            {
                return;
            }

            var headquarter = GetHeadquarterAtPosition(entity.transform.position);

            if (headquarter == null)
            {
                return;
            }

            if (!headquarter.HasMember(player.UserIDString))
            {
                NextTick(() =>
                {
                    SendReply(player, Lang("Headquarter_Deployable_Blocked", player.UserIDString));
                    entity.Kill();
                });
            }
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item == null || playerLoot == null || _freeForAllActive)
            {
                return null;
            }

            var player = item.GetOwnerPlayer();

            if (player == null)
            {
                return null;
            }

            var headquarter = GetHeadquarterAtPosition(player.transform.position);

            if (headquarter == null || !headquarter.HasProtection)
            {
                return null;
            }

            var actualContainer = playerLoot.FindContainer(targetContainer);

            if (actualContainer != null)
            {
                string prefabName = actualContainer?.entityOwner?.ShortPrefabName ?? "unknown";

                if (StorageTypes.Contains(prefabName) && !headquarter.HasMember(player.UserIDString))
                {
                    SendReply(player, Lang("Headquarter_Cant_Move_Storage", player.UserIDString));
                    return false;
                }
            }

            return null;
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null)
            {
                return;
            }

            string prefabName = container?.entityOwner?.ShortPrefabName ?? "unknown";

            if (container == null || !StorageTypes.Contains(prefabName))
            {
                return;
            }

            var hq = GetHeadquarterAtPosition(container.entityOwner.transform.position);

            if (hq != null)
            {
                hq.StorageSlots++;
                var hqConfig = _config.HeadquartersConfig;
                hq.RecalculateProtectionScale(hqConfig.ProtectionPercent, hqConfig.ProtectionPercentMinimum, hqConfig.ProtectionSlotsWithoutPenalty, hqConfig.ProtectionPenaltyPercentPerSlot);
                hq.RefreshMapMarker(_freeForAllActive);
            }
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null)
            {
                return;
            }

            string prefabName = container?.entityOwner?.ShortPrefabName ?? "unknown";

            if (container == null || !StorageTypes.Contains(prefabName))
            {
                return;
            }

            var hq = GetHeadquarterAtPosition(container.entityOwner.transform.position);

            if (hq != null)
            {
                hq.StorageSlots--;
                var hqConfig = _config.HeadquartersConfig;
                hq.RecalculateProtectionScale(hqConfig.ProtectionPercent, hqConfig.ProtectionPercentMinimum, hqConfig.ProtectionSlotsWithoutPenalty, hqConfig.ProtectionPenaltyPercentPerSlot);
                hq.RefreshMapMarker(_freeForAllActive);
            }
        }

        private object OnEntityTakeDamage(BaseVehicleModule entity, HitInfo info)
        {
            if (_freeForAllActive || entity == null || info == null)
            {
                return null;
            }

            string prefabName = entity?.ShortPrefabName ?? "unknown";

            if (StorageTypesPenalizeModules.Contains(prefabName) && info.damageTypes.Has(Rust.DamageType.Decay))
            {
                var headquarter = GetHeadquarterAtPosition(entity.transform.position);
                var vehicleModule = entity as BaseVehicleModule;

                if (headquarter != null && vehicleModule != null && entity.healthFraction < .5)
                {
                    var foundSCs = vehicleModule.children.FindAll((BaseEntity x) => x is StorageContainer && !x.ShortPrefabName.Contains("fuel"));

                    var random = new System.Random();

                    foreach (var scEntity in foundSCs)
                    {
                        var storageContainer = scEntity as StorageContainer;

                        if (storageContainer != null && !storageContainer.inventory.IsEmpty())
                        {
                            storageContainer.inventory.itemList.RemoveAt(random.Next(storageContainer.inventory.itemList.Count));
                        }
                    }

                }

                return null;
            }

            return null;
        }

        private object OnEntityTakeDamage(BuildingPrivlidge entity, HitInfo info)
        {
            if (_freeForAllActive || entity == null || info == null)
            {
                return null;
            }

            var attacker = info?.Initiator?.ToPlayer();

            var headquarter = GetHeadquarterAtPosition(entity.transform.position);

            if (headquarter != null && headquarter.HasProtection && attacker != null)
            {
                SendReply(info.InitiatorPlayer, Lang("Headquarter_Protected_NoDamage", attacker.UserIDString));
                return true;
            }

            return null;
        }


        private object OnEntityTakeDamage(BuildingBlock entity, HitInfo info)
        {
            return HandleBuildingDamage(entity, info);
        }

        private object OnEntityTakeDamage(Door entity, HitInfo info)
        {
            return HandleBuildingDamage(entity, info);
        }

        private object HandleBuildingDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (_freeForAllActive || entity == null || info == null)
            {
                return null;
            }

            var attacker = info?.Initiator?.ToPlayer();

            if (attacker == null || info == null)
            {
                return null;
            }

            bool isAttackerAuthed = attacker.IsBuildingAuthed(entity.transform.position, entity.transform.rotation, entity.bounds);


            if (isAttackerAuthed)
            {
                return null;
            }

            var headquarter = GetHeadquarterAtPosition(entity.transform.position);

            if (headquarter != null)
            {
                if (headquarter.HasProtection)
                {
                    var hqConfig = _config.HeadquartersConfig;
                    headquarter.RecalculateProtectionScale(hqConfig.ProtectionPercent, hqConfig.ProtectionPercentMinimum, hqConfig.ProtectionSlotsWithoutPenalty, hqConfig.ProtectionPenaltyPercentPerSlot);
                    float headquarterScale = headquarter.LastKnownProtectionPercent;
                    float damageScale = Mathf.Max((1f - headquarterScale), 0f);
                    info.damageTypes.ScaleAll(damageScale);
                    headquarter.RefreshMapMarker(_freeForAllActive);

                    if (damageScale < .01f)
                    {
                        SendReply(info.InitiatorPlayer, Lang("Headquarter_Protected_NoDamage", attacker.UserIDString));
                    }

                    return null;
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


            return null;
        }

        private Headquarter GetHeadquarterAtPosition(Vector3 position)
        {
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                if (Vector3.Distance(position, currentHeadquarter.Value.getPosition()) <= _config.HeadquartersConfig.Radius)
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

                            var newHQ = new Headquarter(newFounder, founderPreviousHQ.Name, founderPreviousHQ.PositionX, founderPreviousHQ.PositionY, founderPreviousHQ.PositionZ, founderPreviousHQ.StorageSlots);

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
            else if (_config.HeadquartersConfig.FreeForAllEnabled)
            {
                var timeLeft = _config.HeadquartersConfig.FreeForAllHoursAfterWipe - DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime).TotalHours;
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

        private void RefreshStorageCounts()
        {
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                RefreshHeadquarterStorageCount(currentHeadquarter.Value);
            }
        }

        private void RefreshHeadquarterStorageCount(Headquarter headquarter)
        {
            if (headquarter == null || !headquarter.HasProtection)
            {
                return;
            }

            headquarter.StorageSlots = 0;

            List<StorageContainer> containers = new List<StorageContainer>();
            Vis.Entities<StorageContainer>(headquarter.getPosition(), _config.HeadquartersConfig.Radius, containers);

            foreach (StorageContainer sc in containers.Distinct().ToList())
            {
                string prefabName = sc?.ShortPrefabName ?? "unknown";

                if (sc != null && StorageTypes.Contains(prefabName))
                {
                    headquarter.StorageSlots += sc.inventory.itemList.Count;
                }
            }

            var hqConfig = _config.HeadquartersConfig;
            headquarter.RecalculateProtectionScale(hqConfig.ProtectionPercent, hqConfig.ProtectionPercentMinimum, hqConfig.ProtectionSlotsWithoutPenalty, hqConfig.ProtectionPenaltyPercentPerSlot);
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
            SendReply(player, Lang("Help_Details", player.UserIDString));
            SendReply(player, Lang("Help_Raid", player.UserIDString));
            SendReply(player, "/hq.start " + Lang("Help_Start_Name", player.UserIDString) + " --- " + Lang("Help_Start", player.UserIDString));
            SendReply(player, "/hq.disable-protection --- " + Lang("Help_Disable_Protection", player.UserIDString));
            SendReply(player, "/hq.ffa --- " + Lang("Help_FFA", player.UserIDString));
            SendReply(player, "/hq.check --- " + Lang("Help_Check", player.UserIDString));
        }

        [ChatCommand("hq.start")]
        private void cmdChatHeadquarterStart(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0 || args[0].Length == 0)
            {
                SendReply(player, Lang("Headquarter_Require_Name", player.UserIDString));
                return;
            }

            var hqName = args[0];

            var existingHeadquarterHere = GetHeadquarterAtPosition(player.transform.position);

            if (existingHeadquarterHere != null)
            {
                SendReply(player, Lang("Headquarter_Inside_Headquarter", player.UserIDString));
                return;
            }

            // ensure next to TC
            List<BaseCombatEntity> cblist = new List<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(player.transform.position, _config.HeadquartersConfig.DistanceToTC, cblist);

            bool nextToCupboard = false;
            BuildingPrivlidge tc = null;

            foreach (BaseCombatEntity bp in cblist.Distinct().ToList())
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

                    var hq = new Headquarter(player.UserIDString, hqName, tc.transform.position.x, tc.transform.position.y, tc.transform.position.z);
                    _data.AvailableHeadquarters[player.UserIDString] = hq;

                    RefreshHeadquarterStorageCount(hq);

                    hq.RefreshMapMarker(_freeForAllActive);

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
                var hqConfig = _config.HeadquartersConfig;
                existingHeadquarterHere.RecalculateProtectionScale(hqConfig.ProtectionPercent, hqConfig.ProtectionPercentMinimum, hqConfig.ProtectionSlotsWithoutPenalty, hqConfig.ProtectionPenaltyPercentPerSlot);
                float headquarterScale = existingHeadquarterHere.LastKnownProtectionPercent;
                SendReply(player, Lang("Headquarter_Here_Protection_Rating", player.UserIDString, existingHeadquarterHere.Name, (headquarterScale * 100).ToString()));
                existingHeadquarterHere.RefreshMapMarker(_freeForAllActive);
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
