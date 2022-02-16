## Purpose

Allows players to enjoy a better mid and end game PVP experience in Rust.  This is a hybrid raid protection system.  It is not no raid, it is not noob protect, and it is not PVE.  Players can still raid, be killed, and lose their bases with Headquarters mod.

## How it works

Headquarters mod allows each player to have one main headquarter base which receives added protection from attack.  All their other bases function normally without any form of protection. The headquarter's protection starts out at 100% damage reduction but scales down as players store more items inside the headquarter.  Additionally, the headquarter's location and protection level is shown in the map for all to see.  

* Headquarter protected bases allow players to:
  * Design bases properly, without having to rush through the building process for fear of being raided right away.
  * Allows for more diverse bases and creativity, rather than everyone having the same base designs.
  * Allows casual players to play with mid and end game components (electricity, traps, turrets, etc).
* Creates a two tier raiding system: regular bases, and headquarters.  In order to participate in headquarter raiding, a player must disable their headquarter's protection.  If they're not ready to disable the protection they can still raid non headquarter bases.
* Self balancing protection.  As players grow more powerful they will store more items in their headquarter, this will cause its protection to be decreased.  At this point they either take the decreased protection, or they move some of their stuff to unprotected bases.
* Free for all mode allows an admin to make the last few hours before a wipe as free for all.  In free for all headquarter protections are removed.  Admins can also manually start or stop FFA to allow for raiding events.

## Rules

In order to stop abuse of headquarters, there are a few basic rules:

* A player can belong to only one protected headquarter at any given time.
* Once a player has been a member of a headquarter, they can’t start a new headquarter.
* A player can join a friend’s headquarter at any time by authorizing with its Tool Cupboard.  If the player is already a part of a headquarter this may result in loss of the previous headquarter and or it’s building privilege (if it had other members).
* Only the founder of a headquarter can disable its protection.  Once disabled, it can’t be re-enabled.
* Players can not build deployables inside other player's headquarters (unless they're a member).

## Permissions

* `headquarters.admin`  -- Grants access to run console commands.

## Console Commands

* `hq.clear-all` -- Removes all headquarters permanenetly.
* `hq.start-ffa` -- Starts free for all mode manually.
* `hq.stop-ffa` -- Stops free for all mode manually.
* `hq.remove {player id}` -- Removes the headquarter, along with its founder and members from the plugin's data.  This will allow all associated players to build another headquarter.

## Chat Commands

* `/hq.help` -- Provides a list of help commands.
* `/hq.start myhq` -- Starts a headquarter at your nearest Tool Cupboard with the name "myhq".
* `/hq.disable-protection` -- (Founder Only) Allows you to disable your headquarter's protection. This will allow you to participate in headquarter raiding of other non protected headquarters.
* `/hq.ffa` -- Provides details on how long until free for all is activated.
* `/hq.check` -- Checks if there is a headquarter at this location, and lets the player know the actual protection level of that headquarter.

## Configuration

```json
{
  "HeadquarterConfig": {
    "Radius": 50.0, // How large a headquarter is.. should match the TC's range
    "DistanceToTC": 5.0, // How close to the TC you need to be to start a headquarter.
    "FreeForAllEnabled": true, // Enables free for all.
    "FreeForAllHoursAfterWipe": 144.0 // How long after the last wipe will FFA be enabled.
    "MarkerPrefab": "assets/prefabs/tools/map/genericradiusmarker.prefab",  // Prefab path
    "ProtectionPercent": 100.0, // Default protection level offered to a protected headquarter base.
    "ProtectionPercentMinimum": 10.0, // Lowest protection level offered to a protected headquarter base.
    "ProtectionSlotsWithoutPenalty": 30.0, // Amount of slots filled without reduction to base protection percent.
    "ProtectionPenaltyPercentPerSlot": 2.0 // Amount of reduction to base protection percent from filling one slot of a storage container.
  }
}
```
