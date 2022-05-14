## Purpose

Allows players to enjoy an improved mid and end game PVP experience in Rust.  This is a hybrid raid protection system.  It is not no raid, it is not noob protect, and it is not PVE.  Players can still raid, be killed, and lose their bases with Headquarters mod.

## How it works

Headquarters mod allows each player to have one main headquarter base which receives added protection.  All their other bases function normally without any form of protection. The headquarter's protection starts out at 100% damage reduction, however, this protection decreases gradually as players fill more inventory slots in storage containers inside the headquarter.  As the protection level changes, it is shown on the map for all to see (configurable).  If the player wishes to increase their headquarter's protection level they simply need to take items out of storage there.  It's a self balancing mechanism between protection and storage.  The exact ratio between this balance is up to you through the plugin configuration.

* Headquarter protected bases allow players to:
  * Design bases properly without having to rush through the building process for fear of being raided right away.
  * Allows for more diverse bases and creativity, rather than everyone having the same ultra efficient base designs.
  * Allows casual players a chance to play with mid and end game components (electricity, traps, turrets, etc).
* All players can raid all bases, including headquarters (although it may not be worth it depending on its protection level).
* Free for all mode allows an admin to schedule a period of time where protections are disabled for everyone.  Admins can also manually start or stop FFA temporarily with console commands to allow for raiding events.

## Additional details

* Protection is applied to HQ building blocks, Tool Cupboard (TC), and doors only (it does not protect windows or other deployables).
* A player can belong to only one protected HQ at any given time.
* A player can join or switch HQ at any time by authorizing with the new HQ's Tool Cupboard.  If the player is already a member of a HQ this may result in loss of the previous HQ and or it’s building privilege (if it had other members they will inherit it).
* The founder of a headquarter can elect to disband it.  However, this will come with a penalty waiting time before being able to start a new HQ.
* Players can not build storage deployables (or add items to storage deployables) inside other HQs.
* Vehicles with storage capabilities which are left inside a HQ base will eventually suffer random inventory losses as they decay.
* Headquarters can be destroyed through raiding if the InvulnerableTC option is disabled in the config. 

## Permissions

* `headquarters.admin`  -- Grants access to run console commands.

## Console Commands

* `hq.hide-markers` -- Hides all map markers.
* `hq.show-markers` -- Shows all map markers.
* `hq.clear-all` -- Removes all headquarters permanenetly.
* `hq.start-ffa` -- Starts free for all mode manually.
* `hq.stop-ffa` -- Stops free for all mode manually.
* `hq.remove {founder player's id}` -- Removes the headquarter, along with its founder and members from the plugin's data.  This will allow all member players to build another headquarter.

## Chat Commands

* `/hq.help` -- Provides a list of help commands.
* `/hq.start myhq` -- Starts a headquarter at your nearest Tool Cupboard with the name "myhq".
* `/hq.disband` -- (Founder Only) Allows you to disband your current headquarter.
* `/hq.ffa` -- Provides details on how long until free for all is activated.
* `/hq.check` -- Checks if there is a headquarter at this location, and lets the player know the actual protection level of that headquarter.  Also lets the player know all protection related config values.
* `/hq.teleport` -- If enabled, allows player to teleport to their HQ.

## Configuration

- `Radius`: The radius of the HQ. This should match the Tool Cupboard's range (or slightly smaller).
- `MapMarkersEnabled`: Whether to show map markers on the map.
- `TeleportEnabled`: Whether players can teleport to their HQ (disabled by default).
- `DisbandPenaltyHours`: Number of hours HQ members must wait before being able to start new Headquarters after disbanding their previous HQ.
- `DistanceToTC`: How close to the TC you need to be to start a headquarter (Probably shouldn't modify).
- `InvulnerableTC`: Enabled by default, prevents all damage to HQ TCs.
- `FreeForAllEnabled`: Whether scheduled free for all is enabled or disabled.
- `FreeForAllHoursAfterWipe`: How many hours from the previous wipe until FFA is scheduled to be enabled.
- `MarkerPrefab`: Prefab for marker.  Should not need to be changed unless the game changes.
- `ProtectionPercent`: Protection level offered to a protected headquarter base (without storage penalties).
- `ProtectionPercentMinimum`: Lowest protection level offered to a protected headquarter (even with high item count).
- `ProtectionSlotsWithoutPenalty`: How many slots can be filled before penalties start to accrue.
- `ProtectionPenaltyPercentPerSlot`: Percentage penalty per slot filled past the `ProtectionSlotsWithoutPenalty`.
- `ProtectionConstantSecondsAfterDamage`: Maintains the protection level constant while a raid is happening.  The seconds represents the time it remains constant after the last time the base was damaged.
- `MessagePlayersHeadquarterAttacked`: Whether to message all players when a headquarter is attacked.  Uses `ProtectionConstantSecondsAfterDamage` seconds to determine how often to send the message.
- `MessagePlayersHeadquarterDestroyed`: Whether to message all players when a headquarter is destroyed.
- `UIEnabled`: Whether UI functionality is displayed.
- `UIRefreshRateSeconds`: How often the UI will update after a change occurs to protection.  Default is 0, however if server performance is suffering, you can increase the value.
- `UIAnchorMin`: UI position min.
- `UIAnchorMax`: UI position max.
```json
{
  "HeadquartersConfig": {
    "Radius": 27.5,
    "MapMarkersEnabled": true,
    "TeleportEnabled": false,
    "DisbandPenaltyHours": 3,
    "DistanceToTC": 2.0,
    "InvulnerableTC": false,
    "FreeForAllEnabled": true,
    "FreeForAllHoursAfterWipe": 144.0,
    "MarkerPrefab": "assets/prefabs/tools/map/genericradiusmarker.prefab",
    "ProtectionPercent": 100.0,
    "ProtectionPercentMinimum": 10.0,
    "ProtectionSlotsWithoutPenalty": 30.0,
    "ProtectionPenaltyPercentPerSlot": 1.5,
    "ProtectionConstantSecondsAfterDamage": 300,
    "MessagePlayersHeadquarterAttacked": true,
    "MessagePlayersHeadquarterDestroyed": true,
    "UIEnabled": true,
    "UIRefreshRateSeconds": 0,
    "UIAnchorMin": {
      "X": 0.83,
      "Y": 0.93
    },
    "UIAnchorMax": {
      "X": 0.995,
      "Y": 0.99
    }
  }
}
```