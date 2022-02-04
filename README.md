## Purpose

Allows casual players to enjoy a better mid and end game PVP experience in Rust.  This is a hybrid raid protection system.  It is not no raid, it is not noob protect, and it is not PVE.  Players can still raid, be killed, and lose their bases with Headquarters mod.

## How it works

Headquarters mod allows each player to have one main headquarter base which is protected from attack.  All their other bases remain unprotected.  

A protected headquarter does have a couple of drawbacks though: 

* It’s base location and protection status is shown in the map for all to see.
* It’s members cannot damage any other headquarters (protected, or unprotected).  Although they can still raid non headquarter bases.  
 
If a player has a protected headquarter base and wishes to raid non protected headquarters, they first need to disable their headquarter’s protection.  

Finally, headquarters supports a free for all mode which allows an admin to make the last hours before a wipe scheduled to be free for all.  In free for all no one has headquarter protection.  Admins can also manually start or stop FFA to allow for raiding events.

## Rules

In order to stop abuse of headquarters protection, there are a few basic rules:

* A player can belong to only one protected headquarter at any given time.
* Once a player has been a founder or member of a headquarter they can’t start a new headquarter.
* A player can join a friend’s headquarter at any time by authorizing with its Tool Cupboard.  If the player is already a part of a headquarter this may result in loss of the previous headquarter and or it’s building privilege (if it had other members).
* Once a player has authorized with a Tool Cupboard inside a headquarter, they can’t deauthorize (or clear list) from that base until the headquarter’s protection is disabled.
* Only the founder of a headquarter can disable its protection.  Once disabled, it can’t be re-enabled again.  The protection is gone for good.

## Permissions

* `headquarters.admin`  -- Grants access to run console commands.

## Console Commands

* `hq.clear-all` -- Removes all headquarters permanenetly.
* `hq.start-ffa` -- Starts free for all mode manually.
* `hq.stop-ffa` -- Stops free for all mode manually.
* `hq.remove {player id}` -- Removes the headquarter, along with its founder and members from the plugin's data.  This will allow all associated players to build another headquarter.

## Chat Commands

* `/hq.help` -- Provides a list of help commands.
* `/hq.start` -- Starts a headquarter at your nearest Tool Cupboard.
* `/hq.disable-protection` -- (Founder Only) Allows you to disable your headquarter's protection. This will allow you to participate in headquarter raiding of other non protected headquarters.
* `/hq.ffa` -- Provides details on how long until free for all is activated.
* `/hq.check` -- Checks if there is a headquarter at this location.

## Configuration

```json
{
  "HeadquarterConfig": {
    "Radius": 50.0, // How large a headquarter is.. should match the TC's range
    "DistanceToTC": 5.0, // How close to the TC you need to be to start a headquarter.
    "FreeForAllEnabled": true, // Enables free for all.
    "FreeForAllHoursAfterWipe": 144.0 // How long after the last wipe will FFA be enabled.
    "MarkerPrefab": "assets/prefabs/tools/map/genericradiusmarker.prefab",  // Prefab path
    "ProtectedColor": "#708A43", // Color
    "UnprotectedColor": "#9A4A3B" // Color
  }
}
```
