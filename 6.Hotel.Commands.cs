using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=70
    public partial class Hotel
    {
           #region Chat Commands

        [ChatCommand("hotel_save")]
        void CmdChatHotelSave(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (!EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, "You are not editing a hotel.");
                return;
            }

            var editedHotel = EditHotel[player.UserIDString];

            var removeHotel = _storedData.Hotels.FirstOrDefault(hotel =>
                string.Equals(hotel.hotelName, editedHotel.hotelName, StringComparison.CurrentCultureIgnoreCase));
            if (removeHotel != null)
            {
                _storedData.Hotels.Remove(removeHotel);
                removeHotel.Activate();
            }

            editedHotel.Activate();

            _storedData.Hotels.Add(editedHotel);

            SaveData();
            LoadPermissions();

            CreateMapMarker(editedHotel);

            EditHotel.Remove(player.UserIDString);

            SendReply(player, "Hotel Saved and Closed.");

            RemoveAdminHotelGui(player);
        }

        [ChatCommand("hotel_close")]
        void CmdChatHotelClose(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (!EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, "You are not editing a hotel.");
                return;
            }

            var editedHotel = EditHotel[player.UserIDString];
            foreach (var hotel in _storedData.Hotels.Where(hotel =>
                string.Equals(hotel.hotelName, editedHotel.hotelName, StringComparison.CurrentCultureIgnoreCase)))
            {
                hotel.Activate();
                break;
            }

            EditHotel.Remove(player.UserIDString);

            SendReply(player, "Hotel Closed without saving.");

            RemoveAdminHotelGui(player);
        }

        [ChatCommand("hotel")]
        void CmdChatHotel(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (!EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, GetMsg(PluginMessages.MessageNoHotelHelp, player.UserIDString));
                return;
            }

            var editedHotel = EditHotel[player.UserIDString];

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.Menu1, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu2, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu3, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu4, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu5, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu6, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu7, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu8, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu9, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu10, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu11, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu12, player.UserIDString));
                SendReply(player, GetMsg(PluginMessages.Menu13, player.UserIDString));
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "location":
                        var rad = editedHotel.r ?? "20";

                        var defaultZoneFlags = config.DefaultZoneFlags;
                        var zoneArgs = new List<string> { "name", editedHotel.hotelName, "radius", rad };
                        foreach (var defaultZoneFlag in defaultZoneFlags)
                        {
                            zoneArgs.Add(defaultZoneFlag);
                            zoneArgs.Add("true");
                        }

                        ZoneManager.Call("CreateOrUpdateZone", editedHotel.hotelName, zoneArgs.ToArray(),
                            player.transform.position);

                        (EditHotel[player.UserIDString]).x =
                            player.transform.position.x.ToString(CultureInfo.InvariantCulture);
                        (EditHotel[player.UserIDString]).y =
                            player.transform.position.y.ToString(CultureInfo.InvariantCulture);
                        (EditHotel[player.UserIDString]).z =
                            player.transform.position.z.ToString(CultureInfo.InvariantCulture);

                        SendReply(player, $"Location set to {player.transform.position}");
                        break;
                    case "rentduration":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel rentduration XX");
                            return;
                        }

                        int rd;
                        int.TryParse(args[1], out rd);

                        (EditHotel[player.UserIDString]).rd = rd.ToString();
                        SendReply(player, $"Rent Duration set to {(rd == 0 ? "Infinite" : rd.ToString())}");
                        break;
                    case "rentprice":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel rentprice XX");
                            return;
                        }

                        int rp;
                        if (!int.TryParse(args[1], out rp))
                        {
                            SendReply(player, "/hotel rentprice XX");
                            return;
                        }

                        (EditHotel[player.UserIDString]).e = rp == 0 ? null : rp.ToString();
                        SendReply(player, $"Rent Price set to {(rp == 0 ? "null" : rp.ToString())}");
                        break;
                    case "kickhobos":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel kickhobos [true/false]");
                            return;
                        }

                        bool kickHobos;
                        if (!bool.TryParse(args[1], out kickHobos))
                        {
                            SendReply(player, "/hotel kickhobos [true/false]");
                            return;
                        }

                        (EditHotel[player.UserIDString]).kickHobos = kickHobos;
                        SendReply(player, "Hotel Kicking Hobos set to {0}", args[1]);
                        break;
                    case "showmarker":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel showmarker [true/false]");
                            return;
                        }

                        bool showMarker;
                        if (!bool.TryParse(args[1], out showMarker))
                        {
                            SendReply(player, "/hotel showmarker [true/false]");
                            return;
                        }

                        (EditHotel[player.UserIDString]).showMarker = showMarker;
                        SendReply(player, "Hotel Show Marker set to {0}", args[1]);
                        break;
                    case "rentcurrency":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel rentcurrency XX");
                            return;
                        }

                        int rc;
                        if (int.TryParse(args[1], out rc))
                        {
                            if (rc == 0 && Economics == null)
                            {
                                SendReply(player,
                                    "You don't have economics, please enter an item short name like scrap.");
                                return;
                            }

                            if (rc == 1 && ServerRewards == null)
                            {
                                SendReply(player,
                                    "You don't have Server Rewards, please enter an item short name like scrap.");
                                return;
                            }

                            SendReply(player, $"Rent Currency set to {(rc == 0 ? "Economics" : "Server Rewards")}");
                        }
                        else
                        {
                            var itemName = args[1];
                            var itemDefinition = ItemManager.FindItemDefinition(itemName);
                            if (itemDefinition == null)
                            {
                                SendReply(player, $"Unable to find the currency item {args[1]}");
                                return;
                            }

                            SendReply(player, $"Rent Currency set to {itemDefinition.displayName.translated}");
                        }

                        (EditHotel[player.UserIDString]).currency = args[1];
                        break;
                    case "roomradius":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel roomradius XX");
                            return;
                        }

                        int rad3;

                        int.TryParse(args[1], out rad3);
                        if (rad3 < 1) rad3 = 5;

                        (EditHotel[player.UserIDString]).rr = rad3.ToString();

                        SendReply(player, $"RoomRadius set to {args[1]}");
                        break;
                    case "permission":
                        if (args.Length == 1)
                        {
                            SendReply(player,
                                "/hotel permission \"Permission Name\" => Sets a permission that the player must have to rent in this hotel. put null or false to cancel the permission");
                            return;
                        }

                        var setNewPermission =
                            (args[1].ToLower() == "null" || args[1].ToLower() == "false" || args[1].ToLower() == "0")
                                ? null
                                : args[1];
                        (EditHotel[player.UserIDString]).p = setNewPermission;

                        SendReply(player, $"Permissions set to {setNewPermission ?? "null"}");
                        break;
                    case "npc":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel npc npcId");
                            return;
                        }

                        long npcId;
                        long.TryParse(args[1], out npcId);
                        if (npcId < 1) return;

                        (EditHotel[player.UserIDString]).npc = npcId.ToString();
                        SendReply(player, $"NPC Id hooked to this hotel: {npcId}");
                        break;
                    case "rooms":
                        SendReply(player, "Rooms Refreshing ...");
                        (EditHotel[player.UserIDString]).RefreshRooms();

                        SendReply(player, "Rooms Refreshed");
                        break;
                    case "reset":
                        foreach (var pair in (EditHotel[player.UserIDString]).rooms)
                        {
                            var codeLock = FindCodeLockByRoomId(pair.Key);
                            if (codeLock == null) continue;
                            SendReply(player, "Room Last Renter: {0}, Room Renter: {1}", pair.Value.lastRenter,
                                pair.Value.renter);
                            ResetRoom(codeLock, pair.Value);
                        }

                        break;
                    case "radius":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel radius XX");
                            return;
                        }

                        int rad2;

                        int.TryParse(args[1], out rad2);
                        if (rad2 < 1) rad2 = 20;

                        var zoneArgs2 = new[] { "name", editedHotel.hotelName, "radius", rad2.ToString() };
                        ZoneManager.Call("CreateOrUpdateZone", editedHotel.hotelName, zoneArgs2);

                        (EditHotel[player.UserIDString]).r = rad2.ToString();

                        SendReply(player, $"Radius set to {args[1]}");
                        break;

                    default:
                        SendReply(player, $"Wrong argument {args[0]}");
                        break;
                }
            }

            ShowHotelGrid(player);
            RefreshAdminHotelGui(player);
        }

        [ChatCommand("hotel_list")]
        void CmdChatHotelList(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            SendReply(player, "======= Hotel List ======");
            foreach (HotelData hotel in _storedData.Hotels)
            {
                SendReply(player, $"{hotel.hotelName} - {hotel.rooms.Count}");
            }
        }

        [ChatCommand("hotel_disable")]
        void CmdChatHotelDisable(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.MessageHotelDisableHelp, player.userID));
                return;
            }

            var renter = FindPlayer(args[0]);
            if (renter == null)
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorPlayerNotFound, player.userID));
                return;
            }
            SetPlayerAsRenter(args[0], false);
            SendReply(player, GetMsg(PluginMessages.MessagePlayerNotRenter, player.userID).Replace("{playerName}", renter.Name));

        }

        [ChatCommand("hotel_edit")]
        void CmdChatHotelEdit(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, GetMsg(PluginMessages.MessageAlreadyEditing, player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.MessageHotelEditHelp, player.userID));
                return;
            }

            var hotelName = args[0];
            foreach (var hotel in _storedData.Hotels.Where(hotel =>
                string.Equals(hotel.hotelName, hotelName, StringComparison.CurrentCultureIgnoreCase)))
            {
                hotel.Deactivate();
                if (hotel.x != null && hotel.r != null)
                {
                    foreach (var col in Physics.OverlapSphere(hotel.Pos(), Convert.ToSingle(hotel.r), ConstructionColl))
                    {
                        var door = col.GetComponentInParent<Door>();
                        if (door == null || !door.HasSlot(BaseEntity.Slot.Lock)) continue;

                        door.SetFlag(BaseEntity.Flags.Open, false);
                        door.SendNetworkUpdateImmediate(true);
                    }
                }

                EditHotel.Add(player.UserIDString, hotel);
                break;
            }

            if (!EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player,
                    string.Format(GetMsg(PluginMessages.MessageErrorEditDoesNotExist, player.userID), args[0]));
                return;
            }

            SendReply(player,
                string.Format(GetMsg(PluginMessages.MessageHotelEditEditing, player.userID),
                    EditHotel[player.UserIDString].hotelName));

            RefreshAdminHotelGui(player);
        }

        [ChatCommand("hotel_enable")]
        void CmdChatHotelEnable(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.MessageHotelEnableHelp, player.userID));
                return;
            }

            var renter = FindPlayer(args[0]);
            if (renter == null)
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorPlayerNotFound, player.userID));
                return;
            }
            SetPlayerAsRenter(args[0], true);
            SendReply(player, GetMsg(PluginMessages.MessagePlayerIsRenter, player.userID).Replace("{playerName}", renter.Name));

        }

        private void SetPlayerAsRenter(string playerName, bool isRenter)
        {

            if (isRenter)
            {
                //TODO: Find player and add hotel.renter permission
                Server.Command($"oxide.grant user {playerName} hotel.renter");

            }
            else
            {
                //TODO: Find player and remove hotel.renter permission
                Server.Command($"oxide.revoke user {playerName} hotel.renter");
            }
        }

        [Command("hotelextend")]
        void HotelExtend(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            args[0] = args[0].Replace("'", "");
            CmdChatHotelExtend(player, null, args);
        }

        [ChatCommand("hotel_extend")]
        void CmdChatHotelExtend(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player, "extend"))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.MessageHotelExtendHelp, player.userID));
                return;
            }

            var hotelName = args[0];
            var hotelFound = false;

            foreach (var hotel in _storedData.Hotels.Where(hotel =>
                string.Equals(hotel.hotelName, hotelName, StringComparison.CurrentCultureIgnoreCase)))
            {
                hotelFound = true;
                //We found the Hotel now lets see if player has a room
                //Check to see if player has a room in the hotel and can extend it
                if (CanRentRoom(player, hotel, true))
                {
                    foreach (var room in hotel.rooms.Values)
                    {
                        if (room.renter != player.UserIDString) continue;

                        var duration = double.Parse(hotel.rd);
                        if (duration > 0)
                        {
                            //Charge the player
                            if (hotel.currency == "0" && Economics)
                            {
                                EconomicsWithdraw(player, hotel.Price());
                            }

                            if (hotel.currency == "1" && ServerRewards)
                            {
                                ServerRewardsWithdraw(player, hotel.Price());
                            }

                            if (hotel.currency != "0" && hotel.currency != "1")
                            {
                                var collect = Pool.GetList<Item>();
                                var itemDefinition = ItemManager.FindItemDefinition(hotel.currency);
                                if (itemDefinition != null)
                                {
                                    player.inventory.Take(collect, itemDefinition.itemid, int.Parse(hotel.e));
                                    player.Command("note.inv", itemDefinition.itemid, -1 * int.Parse(hotel.e));
                                }
                            }

                            //extend the Duration
                            room.ExtendDuration(duration);

                            SendReply(player, "Your Room has been extended. Checkout is now in {0}",
                                ConvertSecondsToBetter(room.CheckOutTime() - LogTime()));
                        }
                        else
                        {
                            SendReply(player, "Your Room does not expire, you can not extend this room");
                        }

                        break;
                    }
                }
                else
                {
                    SendReply(player, "Unable to Extend your Room");
                }

                break;
            }

            if (!hotelFound)
            {
                SendReply(player, $"Unable to Find the {hotelName} Hotel");
            }

            SaveData();
        }

        [ChatCommand("hotel_reset")]
        void CmdChatHotelReset(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, GetMsg(PluginMessages.MessageAlreadyEditing, player.userID));
                return;
            }

            _storedData.Hotels = new HashSet<HotelData>();
            SaveData();
            SendReply(player, "Hotels were all deleted");
        }

        [ChatCommand("hotel_remove")]
        void CmdChatHotelRemove(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, GetMsg(PluginMessages.MessageAlreadyEditing, player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.MessageHotelEditHelp, player.userID));
                return;
            }

            var hotelName = args[0];
            HotelData targetHotel = null;
            foreach (var hotel in _storedData.Hotels.Where(hotel =>
                string.Equals(hotel.hotelName, hotelName, StringComparison.CurrentCultureIgnoreCase)))
            {
                hotel.Deactivate();
                targetHotel = hotel;
                break;
            }

            if (targetHotel == null)
            {
                SendReply(player,
                    string.Format(GetMsg(PluginMessages.MessageErrorEditDoesNotExist, player.userID), args[0]));
                return;
            }

            _storedData.Hotels.Remove(targetHotel);
            SaveData();
            SendReply(player, $"Hotel Named: {hotelName} was successfully removed");
        }

        [ChatCommand("rooms")]
        void CmdChatRooms(BasePlayer player, string command, string[] args)
        {
            var hotelFound = false;
            foreach (var hotel in _storedData.Hotels)
            {
                foreach (var pair in hotel.rooms.Where(pair => pair.Value.renter == player.UserIDString))
                {
                    hotelFound = true;
                    SendReply(player, "{0} : {1}", hotel.hotelName,
                        ConvertSecondsToBetter(pair.Value.CheckOutTime() - LogTime()));
                    break;
                }
            }

            if (!hotelFound)
            {
                SendReply(player, "You do not have any rooms rented");
            }
        }

        [ChatCommand("room")]
        void CmdChatRoom(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            var roomId = string.Empty;
            var argsNum = 0;

            if (args.Length > 0)
            {
                var roomLocation = (args[0]).Split(':');
                if (roomLocation.Length == 3)
                    roomId = args[0];
            }

            if (roomId == string.Empty)
            {
                var doorPosition = RayForDoor(player);
                if (doorPosition == default(Vector3))
                {
                    SendReply(player, GetMsg(PluginMessages.MessageMustLookAtDoor, player.userID));
                    return;
                }

                roomId = $"{Mathf.Ceil(doorPosition.x)}:{Mathf.Ceil(doorPosition.y)}:{Mathf.Ceil(doorPosition.z)}";
            }
            else
            {
                argsNum++;
            }

            if (roomId == string.Empty)
            {
                SendReply(player, "Invalid roomId.");
                return;
            }

            HotelData targetHotel;
            Room targetRoom;

            if (!FindRoomById(roomId, out targetHotel, out targetRoom))
            {
                SendReply(player, "No room was detected.");
                return;
            }

            if (args.Length - argsNum == 0)
            {
                SendReply(player, $"Room Id is: <color=orange>{targetRoom.roomId}</color> in hotel: <color=orange>{targetHotel.hotelName}</color>");
                SendReply(player, "Options are:");
                SendReply(player, "<color=green>/room \"optional:roomId\" reset </color> => to reset this room");
                //SendReply(player, "/room \"optional:roomId\
                //" give NAME/STEAMID => to give a player this room");
                SendReply(player,
                    "<color=green>/room \"optional:roomId\" duration</color> Seconds => to set a new duration time for a player (from the time you set the duration)");
                return;
            }

            if (!targetHotel.enabled)
            {
                SendReply(player, GetMsg(PluginMessages.MessageMaintenance, player.UserIDString));
                return;
            }

            switch (args[argsNum])
            {
                case "reset":
                    ResetRoom(targetRoom);
                    SendReply(player, $"The room {targetRoom.roomId} was reset.");
                    break;

                case "duration":
                    if (targetRoom.renter == null)
                    {
                        SendReply(player,
                            $"The room {targetRoom.roomId} has currently no renter, you can't set a duration for it");
                        return;
                    }

                    if (args.Length == argsNum + 1)
                    {
                        var timeLeft = targetRoom.CheckOutTime() - LogTime();
                        SendReply(player,
                            $"The room {targetRoom.roomId} renter will expire in {(targetRoom.CheckOutTime() == 0.0 ? "Unlimited" : ConvertSecondsToBetter(timeLeft))}");
                        return;
                    }

                    double newTimeLeft;
                    if (!double.TryParse(args[argsNum + 1], out newTimeLeft))
                    {
                        SendReply(player, "/room \"optional:roomId\" duration NewTimeLeft");
                        return;
                    }

                    targetRoom.intCheckoutTime = (newTimeLeft + LogTime());
                    targetRoom.checkoutTime = targetRoom.intCheckoutTime.ToString(CultureInfo.InvariantCulture);
                    SaveData();
                    SendReply(player, $"New time left for room Id {targetRoom.roomId} is {newTimeLeft}s");
                    break;

                case "give":
                    if (targetRoom.renter != null)
                    {
                        SendReply(player,
                            $"The room {targetRoom.roomId} is already rented by {targetRoom.renter}, reset the room first to set a new renter");
                        return;
                    }

                    if (args.Length == argsNum + 1)
                    {
                        SendReply(player, "/room \"optional:roomId\" give Player/SteamId");
                        return;
                    }

                    SendReply(player, "Future Feature to Give player the Room");
                    break;

                default:
                    SendReply(player, "This is not a valid option, say /room \"optional:roomId\" to see the options");
                    break;
            }
        }

        [ChatCommand("hotel_new")]
        void CmdChatHotelNew(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowed, player.userID));
                return;
            }

            if (EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, GetMsg(PluginMessages.MessageAlreadyEditing, player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg(PluginMessages.MessageHotelNewHelp, player.userID));
                return;
            }

            var hotelName = args[0];

            if (_storedData.Hotels.Any(x =>
                string.Equals(x.hotelName, hotelName, StringComparison.CurrentCultureIgnoreCase)))
            {
                SendReply(player,
                    string.Format(GetMsg(PluginMessages.MessageErrorAlreadyExist, player.userID), hotelName));
                return;
            }

            var newHotel = new HotelData(hotelName);
            newHotel.Deactivate();

            EditHotel.Add(player.UserIDString, newHotel);

            SendReply(player, string.Format(GetMsg(PluginMessages.MessageHotelNewCreated, player.userID), hotelName));
            RefreshAdminHotelGui(player);
        }

        #endregion

    }
}
