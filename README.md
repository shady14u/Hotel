![alt text](https://i.imgur.com/wXFS9Bw.png?1)

[Click here for a demo](https://www.youtube.com/watch?v=nuZDKD-pwZo)

## Features
**Hotel** is a completely automated room rental system for Rust
- Admins can create rooms, for players to rent, using Server Rewards, Economics, or In-Game Items
- Rooms can be pre configured with the items each renter starts with
- Room duration, cost, currency, permissions, and blacklisted items are all configurable
- Rooms are fully automatic once setup, for collecting rent and tracking players access
- Players are allowed to rent 1 room per Hotel, you can have as many hotels on a server, as you like however.
- Players are ejected from the hotel if sleeping, when rent expires

## Permissions
- `hotel.admin` -- required admin only permission for hotel setup
- `hotel.extend` -- required player permission for `/hotel_extend` command
- `hotel.renter` -- default hotel permission, you can add and assign with hotel.admin permission

## Chat Commands
**Player Commands (oxide permission: hotel.extend):**
- `/rooms` => Lists the players rooms and the time remaining for each
- `/hotel_extend {NAME}` => Extends the players stay, by adding the rent duration to the current checkout date/time, for an additional fee. Allows player to rent their room longer for more Cost.

**Admin Commands (oxide permission: hotel.admin):**
- `/hotel ARG1 ARG2` => Edit the hotel options
- `/hotel_disable {player}` => Revokes Player *hotel.renter* Permission
- `/hotel_enable {player}` => Grants Player *hotel.renter* Permission
- `/hotel_edit {NAME}` => Starts Editing an existing Hotel with the {Name}
- `/hotel_list` => Gets a list of the hotels
- `/hotel_new {NAME}` => Creates a new hotel with the {Name}
- `/hotel_remove {NAME}` => Remove the Hotel named {Name}
- `/hotel_reset` => Remove **ALL** hotels **Use with caution**
- `/room optional:ROOMID reset` => resets the room clear out the renter
- `/room optional:ROOMID duration XX` => sets a NEW duration for the room in seconds (Room must be rented and it only affects current player's room duration)

## Hotel Options
- `/hotel kickhobos [true/false]` => when true any player trying to sleep without owning a room is ejected from the zone
- `/hotel location` => sets the hotel center location to where you stand, and applies the default zone flags from the config
- `/hotel npc NPCID` => sets the NPC that is hooked to this hotel this requires **HumanNPC** 
- `/hotel permission XX` => sets the required permission to rent a room at this hotel (ie. *renter*)
- `/hotel radius XX` => sets the radius of the hotel (the entire structure of the hotel needs to be covered by the zone)
- `/hotel rentduration XX` => Sets the duration of a default rent in this hotel. 0 is infinite
- `/hotel rentprice XXX` => Sets the rent price.
- `/hotel rentcurrency XXX` => Sets the currency used to rent (0 - Economics, 1- Server Rewards, or item shortname)
- `/hotel reset` => resets the hotel data (all players and rooms but keeps the hotel
- `/hotel roomradius XX` => sets the radius of the rooms
- `/hotel rooms` => refreshs the rooms (detects new rooms, deletes rooms if they don't exist anymore, if rooms are in use they won't be effected)
- `/hotel showmarker [true/false]` => toggles display of marker on the map

## Creating a New Hotel:

1) Create and build your hotel and rooms. 
  >**Note:** *All deployables must be seen by the door. You must place a **CODE LOCK** on the door for it to be recognized as a room. If more then 1 door with a code lock, is detectable by a deployable, this deployable will **NOT** be saved as inside a room (so you may place items in corridors)* 
2) use the command `/hotel_new {NAME OF HOTEL}` to create a new hotel in edit mode.
3) Go to the center of your hotel and type: `/hotel location` this should now show a dome and set default zone flags.
4) Set the radius of the hotel zone to cover your hotel: `/hotel radius XX`
5) If you have very very big rooms, you may want to increase the radius of the rooms. Otherwise, you should keep it to 10. `/hotel roomradius 10`
6) Set the duration of the rent in seconds: '/hotel rentduration XX' (default is 86400 = 1day)
7) Set the rent price for the duration: '/hotel rentprice xx' (default is 0 or free)
8) Set the rent currency: '/hotel rentcurrency scrap' (0 - Economics, 1 - Server Rewards, shortname of any item)
9) Set if you want to kick hobos (player who tries to sleep without a room): `/hotel kickhobos true` (true/false)
10) If you want to show a map marker use `/hotel showmarker true` (true/false)
11) Take a snapshot of the rooms and their contents with `/hotel rooms`. This will detect all the rooms and all the default deployables that will remain when the room resets. 
12) Save your hotel with `/hotel_save` => DONT FORGET THIS!!! Make sure that all the deployables that you have placed are correctly detected in everyroom (review your hotel.json **data file**). If they are not, you might want to make sure that the deployable can be seen by the door.
>Now people may use the hotel. Everything is automated so you shouldn't have to manage it.

## Editing a Hotel:
      **Example** Adding an NPC via HumanNpc:
1) Create a new NPC with `/npc_add`
2) Get the npc Id with `/npc_list`
3) Start edit the hotel with `/hotel_edit {NAME OF HOTEL}`
4) Update the setting for the npc `/hotel npc {NPCID}`
5) Save changes to the hotel `/hotel_save`

## Zone Management:
>Zone manager controls setting within the zone.
1) Use /zone_edit {HOTELNAME} - to edit the hotel's zone.
2) Type /zone flags - to open the zones configuration gui settings

I recommend using:
*  NoBuild
*  UnDestr
*  NoCup
*  NoDecay
*  NoEntityPickup
*  NoStash
*  NoPlayerLoot
*  LootSelf

>Just be mindful of sleepers in your hotel, if the heli was to shoot rockets at it will they live, (do you need PvE God, PvP God, or SleepGod On)?

Config File:
```
{
  "adminGuiJson": "[ {\"name\": \"HotelAdmin\",\"parent\": \"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1 0.1 0.1 0.7\",},{\"type\":\"RectTransform\",\"anchormin\": \"{xmin} {ymin}\",\"anchormax\": \"{xmax} {ymax}\"}]},{\"parent\": \"HotelAdmin\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{msg}\",\"fontSize\":15,\"align\": \"MiddleLeft\",},{\"type\":\"RectTransform\",\"anchormin\": \"0.1 0.1\",\"anchormax\": \"1 1\"}]}]",
  "authLevel": 2,
  "enterZoneShowPlayerGUI": false,
  "enterZoneShowRoom": false,
  "hotelPanel": {
    "AnchorX": "Left",
    "AnchorY": "Bottom",
    "Autoload": true,
    "Available": true,
    "BackgroundColor": "0 0 0 0",
    "Dock": "TopLeftDock",
    "Height": 0.95,
    "Image": {
      "AnchorX": "Left",
      "AnchorY": "Bottom",
      "Available": true,
      "BackgroundColor": "0 0 0 0",
      "Dock": "TopLeftDock",
      "Height": 0.8,
      "Margin": "0 0.05 0.1 0.05",
      "Order": 1,
      "Url": "https://i.imgur.com/XHm7WGb.png",
      "Width": 0.15
    },
    "Margin": "0 0 0 0.01",
    "Order": 8,
    "Text": {
      "Align": "MiddleCenter",
      "AnchorX": "Left",
      "AnchorY": "Bottom",
      "Available": true,
      "BackgroundColor": "0 0 0 0",
      "Content": "Hotel Rooms",
      "Dock": "TopLeftDock",
      "FontColor": "1 1 1 1",
      "FontSize": 10,
      "Height": 1.0,
      "Margin": "0 0.02 0 0 ",
      "Order": 2,
      "Width": 0.85
    },
    "Width": 0.4
  },
  "KickHobos": true,
  "mapMarker": "\t\t\t{name} Hotel\r\n{fnum} of {rnum} Rooms Available\r\n{rp} {rc} per {rd} Seconds",
  "mapMarkerColor": "#710AC1",
  "mapMarkerColorBorder": "#5FCEA8",
  "mapMarkerRadius": 0.25,
  "openDoorPlayerGUI": true,
  "openDoorShowRoom": false,
  "panelTimeOut": 10,
  "panelXMax": "0.6",
  "panelXMin": "0.3",
  "panelYMax": "0.95",
  "panelYMin": "0.7",
  "playerGuiJson": "[{\"name\": \"HotelPlayer\",\"parent\": \"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1 0.1 0.1 0.7\",},{\"type\":\"RectTransform\",\"anchormin\": \"{pxmin} {pymin}\",\"anchormax\": \"{pxmax} {pymax}\"}]},{\"parent\": \"HotelPlayer\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{msg}\",\"fontSize\":15,\"align\": \"MiddleLeft\",},{\"type\":\"RectTransform\",\"anchormin\": \"0.1 0.1\",\"anchormax\": \"1 1\"}]}]",
  "useNPCShowPlayerGUI": true,
  "useNPCShowRoom": false,
  "xMax": "1.0",
  "xMin": "0.65",
  "yMax": "0.9",
  "yMin": "0.6",
  "blackListGuiJson": "[{\"name\": \"HotelBlackList\",\"parent\": \"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1 0.1 0.1 0.7\",},{\"type\":\"RectTransform\",\"anchormin\": \"{pxmin} {pymin}\",\"anchormax\": \"{pxmax} {pymax}\"}]},{\"parent\": \"HotelBlackList\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{msg}\",\"fontSize\":15,\"align\": \"MiddleLeft\",},{\"type\":\"RectTransform\",\"anchormin\": \"0.1 0.1\",\"anchormax\": \"1 1\"}]}]",
  "blackList": [
    "explosive.timed"
  ],
  "defaultZoneFlags": [
    "lootself",
    "nobuild",
    "nocup",
    "nodecay",
    "noentitypickup",
    "noplayerloot",
    "nostash",
    "notrade",
    "pvpgod",
    "sleepgod",
    "undestr"
  ]
}
```
This plugin was originally developed by Reneb. Much respect and admiration should go to this developer.
