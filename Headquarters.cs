using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Headquarters", "digital0iced0", "0.1.7")]
    [Description("Allows players to have one protected headquarter base.")]
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

            public bool MapMarkersEnabled { get; set; } = true;

            public bool TeleportEnabled { get; set; } = false;

            public float DisbandPenaltyHours { get; set; } = 3f;

            public float DistanceToTC { get; set; } = 2f;

            public bool InvulnerableTC { get; set; } = true;

            public bool FreeForAllEnabled { get; set; } = true;

            public float FreeForAllHoursAfterWipe { get; set; } = 144f;

            public string MarkerPrefab { get; set; } = "assets/prefabs/tools/map/genericradiusmarker.prefab";

            public float ProtectionPercent { get; set; } = 100f;

            public float ProtectionPercentMinimum { get; set; } = 10f;

            public float ProtectionSlotsWithoutPenalty { get; set; } = 30f;

            public float ProtectionPenaltyPercentPerSlot { get; set; } = 1.5f;

            public int ProtectionConstantSecondsAfterDamage { get; set; } = 300;

            public bool MessagePlayersHeadquarterAttacked { get; set; } = true;

            public bool MessagePlayersHeadquarterDestroyed { get; set; } = true;
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
                ["Headquarter_Exists_Cant_Clear_List"] = "A HQ exists at this location.  You can't clear its privilege list.",
                ["Headquarter_Exists_Cant_Deauth"] = "A HQ exists at this location.  You must disband it before being able to deauthorize from it's Tool Cupboard.",
                ["Headquarter_Inside_Headquarter"] = "You can't create a HQ inside another HQ.",
                ["Headquarter_Not_Inside"] = "You're not inside a HQ.",
                ["Headquarter_Start_Near_TC"] = "You must stand next to your base's Tool Cupboard to start a HQ.",
                ["Headquarter_Successful_Start"] = "You have started a HQ at this base! You can invite others to join your HQ by having them authenticate at the Tool Cupboard.  To keep your HQ base secure, put a lock on your Tool Cupboard.",
                ["Headquarter_Already_Started"] = "You have already started or joined a HQ.  This disqualifies you from creating a new HQ. However, you can join another player's HQ by authenticating at their Tool Cupboard.",
                ["Headquarter_Founder_Quit_Promoted"] = "Your previous HQ had members, therefore, one of them has been promoted to lead it.  You have been removed from the headquarter and deauthed from its Tool Cupboard.",
                ["Headquarter_Founder_Quit_Empty"] = "Your previous HQ has been disbanded.  Since you were its only member you still maintain privilege at the base.",
                ["Headquarter_Disband_Started"] = "You have started the disband process for your HQ.  This may take some time to complete.  Once it is complete you will be able to start a new HQ.  In the mean time your base will not receive any HQ protection.",
                ["Headquarter_Only_Founder"] = "Only the founder of a HQ can perform this action.",
                ["Headquarter_Found_Count"] = "Found {0} HQs.",
                ["Headquarter_Near_Found"] = "Headquarter {0} near {1}.",
                ["Headquarter_Name_Exists"] = "This name is already taken.  Please try another.",
                ["Headquarter_Already_Member"] = "You're already part of this HQ.",
                ["Headquarter_Cleared"] = "All HQs have been removed.  Protections are disabled.",
                ["Headquarter_Here_Protection_Rating"] = "You're in {0}'s HQ! ({1}%).",
                ["Headquarter_Empty_Here"] = "There isn't a HQ at this position.",
                ["Headquarter_Require_Name"] = "You must provide a name for your HQ.",
                ["Headquarter_Deployable_Blocked"] = "You can't deploy this item inside someone else's HQ.",
                ["Headquarter_Disband_Incomplete"] = "Your headquarter is still in the process of disbanding.  Please try again later.",
                ["Headquarter_Being_Attacked"] = "Allies of {0}, the time to honor your alliance has come!  {1} has initiated an attack!",
                ["Headquarter_Destroyed"] = "{0} has fallen!",

                ["Free_For_All_Active"] = "<color=green>HQ free for all is active! HQ protections are disabled!</color>",
                ["Free_For_All_Stopped"] = "<color=red>HQ free for all is deactivated!</color>",
                ["Free_For_All_Status"] = "HQ free for all is expected {0}",
                ["Free_For_All_Only_Admin"] = "HQ free for all is deactivated.  Only an admin can enable it.",

                ["Cmd_Permission"] = "You don't have permission to use that command",
                ["Cmd_Remove_Heaquarter_Founder_Missing"] = "Please provide the user id of the player whose HQ you wish to remove.",
                ["Cmd_Headquarter_Removed"] = "The founder's HQ has been removed.",
                ["Cmd_Headquarter_Remove_Fail"] = "Could not find a HQ belonging to this founder.",

                ["Help_Welcome"] = "Welcome to Headquarters! This mod allows you to provide protection for one of your bases by designating it your headquarter (HQ).",
                ["Help_Details"] = "A few simple things to keep in mind: Protection is applied to your HQ building blocks, Tool Cupboard (TC), and doors only (it does not protect windows or other deployables).  You can only belong to one HQ at any given time. You can switch HQ by authenticating at someone else's TC but you will lose your previous HQ.  If you place too many items inside your HQ it will reduce its protection level.  Removing items from the HQ will increase its protection again.",
                ["Help_Raid"] = "You can raid headquarters and regular bases.  However, it may not be worth it to raid a headquarter with a high protection level.",
                ["Help_Start"] = "Starts a named HQ at one of your bases' Tool Cupboard.",
                ["Help_Start_Name"] = "(name)",
                ["Help_Disband"] = "(Founder Only) Allows you to start the disband process for your HQ. During this time your HQ will not receive any protection, and you will not be able to start a new one.  Once the process is complete you will be able to start a new HQ.",
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
            public bool IsActive { get; set; }
            public List<string> MemberIds { get; set; } = new List<string>();
            [JsonIgnore]
            public MapMarkerGenericRadius marker;
            public DateTime DisbandedAt { get; set; }
            public float LastKnownProtectionPercent { get; set; } = 1;
            public bool MapMarkerEnabled { get; set; }
            public DateTime LastMarkerRefresh { get; set; }

            public DateTime LastDamaged { get; set; }

            public Headquarter(string user, string name, float positionX, float positionY, float positionZ, int storageSlots = 0, bool mapMarkerEnabled = true)
            {
                this.FounderId = user;
                this.Name = name;
                this.PositionX = positionX;
                this.PositionY = positionY;
                this.PositionZ = positionZ;
                this.IsActive = true;
                this.StorageSlots = storageSlots;
                this.MapMarkerEnabled = mapMarkerEnabled;
                this.LastDamaged = DateTime.Now.AddDays(-1);
                this.LastMarkerRefresh = DateTime.Now.AddDays(-1);
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

            public void MarkDamaged()
            {
                this.LastDamaged = DateTime.UtcNow;
            }

            public void CreateMapMarker(bool freeForAllActive = false)
            {
                if (marker != null || !MapMarkerEnabled)
                {
                    return;
                }

                marker = GameManager.server.CreateEntity(_config.HeadquartersConfig.MarkerPrefab, getPosition()) as MapMarkerGenericRadius;

                if (marker != null)
                {
                    marker.alpha = 0.6f;
                    marker.name = this.Name;

                    if (IsActive)
                    {
                        marker.color1 = (freeForAllActive) ? Color.red : Color.yellow;
                        marker.color2 = (freeForAllActive) ? Color.red : getProtectionColor();
                    }
                    else
                    {
                        marker.color1 = Color.black;
                        marker.color2 = Color.black;
                    }

                    marker.radius = 0.2f;
                    marker.Spawn();
                    marker.SendUpdate();
                }
            }

            public void RemoveMapMarker()
            {
                if (!MapMarkerEnabled)
                {
                    return;
                }

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
                if (!MapMarkerEnabled)
                {
                    return;
                }

                if (marker != null)
                {
                    marker.SendNetworkUpdate();
                    marker.SendUpdate();
                }
            }

            public void RefreshMapMarker(bool freeForAllActive = false, bool forceUpdate = false)
            {
                if (!MapMarkerEnabled)
                {
                    return;
                }

                if (forceUpdate || DateTime.UtcNow.Subtract(LastMarkerRefresh).TotalSeconds > 10)
                {
                    RemoveMapMarker();
                    CreateMapMarker(freeForAllActive);
                    this.LastMarkerRefresh = DateTime.UtcNow;
                }
            }

            private Color getProtectionColor()
            {
                if (!IsActive)
                {
                    return Color.red;
                }

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

            public void RecalculateProtectionScale(HeadquartersConfig c)
            {
                if (!IsActive)
                {
                    this.LastKnownProtectionPercent = 0;
                    return;
                }

                if (DateTime.UtcNow.Subtract(LastDamaged).TotalSeconds < c.ProtectionConstantSecondsAfterDamage)
                {
                    return;
                }

                this.LastKnownProtectionPercent = Mathf.Min(c.ProtectionPercent, Mathf.Max((c.ProtectionPercent - ((this.StorageSlots - c.ProtectionSlotsWithoutPenalty) * c.ProtectionPenaltyPercentPerSlot)), c.ProtectionPercentMinimum)) / 100;
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

            if (headquarter == null || !headquarter.IsActive)
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
            if (container == null || item == null || container.entityOwner == null || container.entityOwner.transform == null || container.entityOwner.transform.position == null)
            {
                return;
            }

            string prefabName = container.entityOwner.ShortPrefabName ?? "unknown";

            if (!StorageTypes.Contains(prefabName))
            {
                return;
            }

            var hq = GetHeadquarterAtPosition(container.entityOwner.transform.position);

            if (hq != null)
            {
                hq.StorageSlots++;
                hq.RecalculateProtectionScale(_config.HeadquartersConfig);
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
                hq.RecalculateProtectionScale(_config.HeadquartersConfig);
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

                if (headquarter != null && vehicleModule != null && entity.healthFraction < .25)
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

        private void OnEntityDeath(BuildingPrivlidge entity, HitInfo info)
        {
            if (entity == null)
            {
                return;
            }

            var headquarter = GetHeadquarterAtPosition(entity.transform.position);

            if (headquarter != null && headquarter.IsActive)
            {
                headquarter.IsActive = false;
                headquarter.DisbandedAt = DateTime.UtcNow;
                headquarter.RefreshMapMarker(_freeForAllActive, true);

                MessageAllPlayersHeadquarterDestroyed(headquarter);
            }
        }

        private object OnEntityTakeDamage(BuildingPrivlidge entity, HitInfo info)
        {
            if (entity == null || info == null)
            {
                return null;
            }

            if (!_config.HeadquartersConfig.InvulnerableTC)
            {
                return HandleBuildingDamage(entity, info);
            }

            var headquarter = GetHeadquarterAtPosition(entity.transform.position);

            if (headquarter != null && headquarter.IsActive)
            {
                var attacker = info?.Initiator?.ToPlayer();

                if (attacker != null)
                {
                    SendReply(attacker, Lang("Headquarter_Protected_NoDamage", attacker.UserIDString));
                }

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
            if (_freeForAllActive || entity == null || info == null || !info.damageTypes.IsConsideredAnAttack())
            {
                return null;
            }

            var attacker = info.Initiator?.ToPlayer();

            if (attacker != null)
            {
                bool isAttackerAuthed = attacker.IsBuildingAuthed(entity.transform.position, entity.transform.rotation, entity.bounds);

                if (isAttackerAuthed)
                {
                    return null;
                }
            }

            var headquarter = GetHeadquarterAtPosition(entity.transform.position);

            if (headquarter != null && headquarter.IsActive)
            {
                headquarter.RecalculateProtectionScale(_config.HeadquartersConfig);
                float headquarterScale = headquarter.LastKnownProtectionPercent;
                float damageScale = Mathf.Max((1f - headquarterScale), 0f);
                info.damageTypes.ScaleAll(damageScale);
                headquarter.RefreshMapMarker(_freeForAllActive);

                if (damageScale < .01f && attacker != null)
                {
                    SendReply(info.InitiatorPlayer, Lang("Headquarter_Protected_NoDamage", attacker.UserIDString));
                }
                else if (damageScale > .01)
                {
                    MessageAllPlayersHeadquarterBeingAttacked(headquarter, attacker);
                }

                headquarter.MarkDamaged();

                return null;
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

            if (headquarter != null && headquarter.IsActive)
            {
                SendReply(player, Lang("Headquarter_Exists_Cant_Clear_List", player.UserIDString));
                return true;
            }

            return null;
        }

        object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var headquarter = GetHeadquarterAtPosition(player.transform.position);

            if (headquarter != null && headquarter.IsActive)
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

                            var newHQ = new Headquarter(newFounder, founderPreviousHQ.Name, founderPreviousHQ.PositionX, founderPreviousHQ.PositionY, founderPreviousHQ.PositionZ, founderPreviousHQ.StorageSlots, founderPreviousHQ.MapMarkerEnabled);

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
            if (!_config.HeadquartersConfig.MapMarkersEnabled)
            {
                return;
            }

            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.CreateMapMarker();
            }
        }

        private void RefreshMapMarkers()
        {
            if (!_config.HeadquartersConfig.MapMarkersEnabled)
            {
                return;
            }

            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.RefreshMapMarker(_freeForAllActive);
            }
        }

        private void RemoveMapMarkers()
        {
            if (!_config.HeadquartersConfig.MapMarkersEnabled)
            {
                return;
            }

            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.RemoveMapMarker();
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
            if (headquarter == null || !headquarter.IsActive)
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

            headquarter.RecalculateProtectionScale(_config.HeadquartersConfig);
        }

        private void DeauthPlayerFromTC(BasePlayer player, BuildingPrivlidge privilege)
        {
            var found = privilege.authorizedPlayers.Find(e => e.userid == player.userID);

            if (found != null)
            {
                privilege.authorizedPlayers.Remove(found);
            }
        }

        private void MessageAllPlayersHeadquarterBeingAttacked(Headquarter hq, BasePlayer attacker)
        {
            if (attacker != null && _config.HeadquartersConfig.MessagePlayersHeadquarterAttacked && DateTime.UtcNow.Subtract(hq.LastDamaged).TotalSeconds > _config.HeadquartersConfig.ProtectionConstantSecondsAfterDamage)
            {
                Headquarter attackerHQ = GetPlayerHeadquarter(attacker);
                String attackerString = attackerHQ == null ? attacker.displayName : attackerHQ.Name;

                foreach (var player in BasePlayer.activePlayerList)
                {
                    PrintToChat(player, Lang("Headquarter_Being_Attacked", player.UserIDString, hq.Name, attackerString));
                }
            }
        }

        private void MessageAllPlayersHeadquarterDestroyed(Headquarter hq)
        {
            if (_config.HeadquartersConfig.MessagePlayersHeadquarterDestroyed)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    PrintToChat(player, Lang("Headquarter_Destroyed", player.UserIDString, hq.Name));
                }
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
            SendReply(player, "/hq.disband --- " + Lang("Help_Disband", player.UserIDString));
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

            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                if (currentHeadquarter.Value.Name.ToLower() == hqName.ToLower())
                {
                    SendReply(player, Lang("Headquarter_Name_Exists", player.UserIDString));
                    return;
                }
            }

            var isNotAssociatedWithHeadquarter = (!IsFounder(player) && !IsMember(player));

            if (!isNotAssociatedWithHeadquarter)
            {
                var playerHQ = GetPlayerHeadquarter(player);
                var isDisbandComplete = (!playerHQ.IsActive && DateTime.UtcNow.Subtract(playerHQ.DisbandedAt).TotalHours > _config.HeadquartersConfig.DisbandPenaltyHours);

                if (isDisbandComplete)
                {
                    playerHQ.RemoveMapMarker();
                    playerHQ.MemberIds.ForEach(memberId => _data.MemberPlayers.Remove(memberId));
                    _data.AvailableHeadquarters.Remove(playerHQ.FounderId);
                    isNotAssociatedWithHeadquarter = true;
                }
                else if (!playerHQ.IsActive)
                {
                    SendReply(player, Lang("Headquarter_Disband_Incomplete", player.UserIDString));
                    return;
                }
            }

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


            // If this user is not associated with a headquarter
            if (isNotAssociatedWithHeadquarter)
            {
                if (tc != null)
                {
                    ((BuildingPrivlidge)tc).authorizedPlayers.Clear();
                    ((BuildingPrivlidge)tc).authorizedPlayers.Add(new ProtoBuf.PlayerNameID { username = player.name, userid = player.userID });

                    var mapMarkersEnabled = _config.HeadquartersConfig.MapMarkersEnabled;
                    var hq = new Headquarter(player.UserIDString, hqName, player.transform.position.x, player.transform.position.y, player.transform.position.z, 0, mapMarkersEnabled);
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

        [ChatCommand("hq.disband")]
        private void cmdChatHeadquarterDisband(BasePlayer player, string command)
        {
            if (IsFounder(player))
            {
                _data.AvailableHeadquarters[player.UserIDString].IsActive = false;
                _data.AvailableHeadquarters[player.UserIDString].DisbandedAt = DateTime.UtcNow;
                _data.AvailableHeadquarters[player.UserIDString].RefreshMapMarker(_freeForAllActive);
                SendReply(player, Lang("Headquarter_Disband_Started", player.UserIDString));
                return;
            }
            else
            {
                SendReply(player, Lang("Headquarter_Only_Founder", player.UserIDString));
                return;
            }
        }

        [ChatCommand("hq.list")]
        private void cmdChatHeadquarterList(BasePlayer player, string command)
        {
            SendReply(player, Lang("Headquarter_Found_Count", player.UserIDString, _data.AvailableHeadquarters.Count.ToString()));
            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                SendReply(player, Lang("Headquarter_Near_Found", player.UserIDString, ((currentHeadquarter.Value.IsActive && !_freeForAllActive) ? "(" + Lang("Protected", player.UserIDString) + ")" : "(" + Lang("Unprotected", player.UserIDString) + ")"), GetGrid(currentHeadquarter.Value.getPosition())));
            }
        }

        [ChatCommand("hq.teleport")]
        private void cmdChatHeadquarterTeleport(BasePlayer player, string command)
        {

            if (_config.HeadquartersConfig.TeleportEnabled)
            {
                var playerHQ = GetPlayerHeadquarter(player);

                if (playerHQ != null)
                {
                    player.transform.position = playerHQ.getPosition();
                }
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
                existingHeadquarterHere.RecalculateProtectionScale(_config.HeadquartersConfig);
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

        [ConsoleCommand("hq.hide-markers")]
        private void cmdConsoleHideMarkers(ConsoleSystem.Arg arg)
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

            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.RemoveMapMarker();
                currentHeadquarter.Value.MapMarkerEnabled = false;
            }
        }

        [ConsoleCommand("hq.show-markers")]
        private void cmdConsoleShowMarkers(ConsoleSystem.Arg arg)
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

            foreach (KeyValuePair<string, Headquarter> currentHeadquarter in _data.AvailableHeadquarters)
            {
                currentHeadquarter.Value.MapMarkerEnabled = true;
                currentHeadquarter.Value.RecalculateProtectionScale(_config.HeadquartersConfig);
                currentHeadquarter.Value.RefreshMapMarker(_freeForAllActive);
            }
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