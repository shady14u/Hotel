using System.Collections.Generic;

namespace Oxide.Plugins
{
    //Define:FileOrder=30
    public partial class Hotel
    {
        private static class PluginMessages
        {
            public const string MessageHotelDisableHelp = "MessageHotelDisableHelp";
            public const string MessagePlayerIsRenter = "MessagePlayerIsRenter";
            public const string MessagePlayerNotRenter = "MessagePlayerNotRenter";
            public const string MessageHotelEnableHelp = "MessageHotelEnableHelp";
            public const string MessageErrorPlayerNotFound = "MessageErrorPlayerNotFound";
            public const string MessageAlreadyEditing = "MessageAlreadyEditing";
            public const string MessageNoHotelHelp = "MessageNoHotelHelp";
            public const string MessageHotelNewHelp = "MessageHotelNewHelp";
            public const string MessageHotelEditHelp = "MessageHotelEditHelp";
            public const string MessageHotelEditEditing = "MessageHotelEditEditing";
            public const string MessageErrorAlreadyExist = "MessageErrorAlreadyExist";
            public const string MessageErrorNotAllowed = "MessageErrorNotAllowed";
            public const string MessageErrorEditDoesNotExist = "MessageErrorEditDoesNotExist";
            public const string MessageMaintenance = "MessageMaintenance";
            public const string MessageErrorUnavailableRoom = "MessageErrorUnavailableRoom";
            public const string MessageHotelNewCreated = "MessageHotelNewCreated";
            public const string MessageErrorNotAllowedToEnter = "MessageErrorNotAllowedToEnter";
            public const string MessageHotelExtendHelp = "MessageHotelExtendHelp";
            public const string MessageErrorAlreadyGotRoom = "MessageErrorAlreadyGotRoom";
            public const string MessageErrorPermissionsNeeded = "MessageErrorPermissionsNeeded";
            public const string MessageCouldNotFindToExtend = "MessageCouldNotFindToExtend";
            public const string MessageRentUnlimited = "MessageRentUnlimited";
            public const string MessageRentTimeLeft = "MessageRentTimeLeft";
            public const string MessagePayedRent = "MessagePayedRent";
            public const string MessageErrorNotEnoughCoins = "MessageErrorNotEnoughCoins";
            public const string MessageErrorNotEnoughRp = "MessageErrorNotEnoughRP";
            public const string MessageErrorNotEnoughItems = "MessageErrorNotEnoughItems";
            public const string GuiBoardAdmin = "GUIBoardAdmin";
            public const string GuiBoardBlackList = "GuiBoardBlackList";
            public const string GuiBoardPlayer = "GUIBoardPlayer";
            public const string GuiBoardPlayerRoom = "GUIBoardPlayerRoom";
            public const string GuiBoardPlayerMaintenance = "GUIBoardPlayerMaintenance";
            public const string MessageMustLookAtDoor = "MessageMustLookAtDoor";
            public const string Menu1 = "Menu1";
            public const string Menu2 = "Menu2";
            public const string Menu3 = "Menu3";
            public const string Menu4 = "Menu4";
            public const string Menu5 = "Menu5";
            public const string Menu6 = "Menu6";
            public const string Menu7 = "Menu7";
            public const string Menu8 = "Menu8";
            public const string Menu9 = "Menu9";
            public const string Menu10 = "Menu10";
            public const string Menu11 = "Menu11";
            public const string Menu12 = "Menu12";
            public const string Menu13 = "Menu13";
            public const string ExtendButtonText = "ExtendButtonText";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [PluginMessages.MessageHotelDisableHelp] =
                    "You must enter the name/id of the person you want to disable renting hotels: <color=green>/hotel_disable \"Player Name/Id\"</color>",
                [PluginMessages.MessagePlayerNotRenter] =
                    "Player <color=green>{playerName}</color> revoked from the <color=green>hotel.renter</color> permission.",
                [PluginMessages.MessagePlayerIsRenter] =
                    "Player <color=green>{playerName}</color> granted the <color=green>hotel.renter</color> permission.",
                [PluginMessages.MessageHotelEnableHelp] =
                   "You must enter the name/id of the person you want to enable renting hotels: <color=green>/hotel_enable \"Player Name/Id\"</color>",
                [PluginMessages.MessageErrorPlayerNotFound] =
                    "Player was not found.",
                [PluginMessages.MessageAlreadyEditing] =
                    "You are already editing a hotel. You must close or save it first.",
                [PluginMessages.MessageNoHotelHelp] =
                    "You are not editing a hotel. Create a new one with <color=green>/hotel_new</color>, or edit an existing one with <color=green>/hotel_edit</color>",
                [PluginMessages.MessageHotelNewHelp] =
                    "You must select a name for the new hotel: <color=green>/hotel_new \"Hotel Name\"</color>",
                [PluginMessages.MessageHotelEditHelp] =
                    "You must select the name of the hotel you want to edit: <color=green>/hotel_edit \"Hotel Name\"</color>",
                [PluginMessages.MessageHotelEditEditing] =
                    "You are editing the hotel named: <color=purple>{0}</color>. Now say <color=green>/hotel</color> to continue configuring your hotel. Note that no one can register/leave the hotel while you are editing it.",
                [PluginMessages.MessageErrorAlreadyExist] = "{0} is already the name of a hotel",
                [PluginMessages.MessageErrorNotAllowed] = "You are not allowed to use this command",
                [PluginMessages.MessageErrorEditDoesNotExist] = "The hotel \"<color=purple>{0}</color>\" doesn't exist",
                [PluginMessages.MessageMaintenance] =
                    "This Hotel is under maintenance by the admin, you may not open this door at the moment",
                [PluginMessages.MessageErrorUnavailableRoom] =
                    "This room is unavailable, seems like it wasn't set correctly",
                [PluginMessages.MessageHotelNewCreated] =
                    "You've created a new Hotel named: <color=purple>{0}</color>. Now say <color=green>/hotel</color> to continue configuring your hotel.",
                [PluginMessages.MessageErrorNotAllowedToEnter] =
                    "You are not allowed to enter this room, it's already being used my someone else",
                [PluginMessages.MessageCouldNotFindToExtend] = "Could not find your room to extend, in the {0} hotel",
                [PluginMessages.MessageHotelExtendHelp] =
                    "You must enter the name of the hotel you want to extend your stay at: <color=green>/hotel_extend \"Hotel Name\"</color>",
                [PluginMessages.MessageErrorAlreadyGotRoom] = "You already have a room in this hotel!",
                [PluginMessages.MessageErrorPermissionsNeeded] =
                    "You must have the <color=purple>{0}</color> permission to rent a room here",
                [PluginMessages.MessageRentUnlimited] = "You now have access to this room for an unlimited time",
                [PluginMessages.MessageRentTimeLeft] =
                    "You now have access to this room. You are allowed to keep this room for <color=purple>{0}</color>",
                [PluginMessages.MessagePayedRent] = "You payed for this room <color=purple>{0}</color> coins",
                [PluginMessages.MessageErrorNotEnoughCoins] =
                    "This room costs <color=purple>{0}</color> coins. You only have <color=purple>{1}</color> coins",
                [PluginMessages.MessageErrorNotEnoughRp] =
                    "This room costs <color=purple>{0}</color> RP. You only have<color=purple>{1}</color> Reward Points",
                [PluginMessages.MessageErrorNotEnoughItems] =
                    "Not enough <color=purple>{0}</color>(s) for this room, you need <color=purple>{1}</color> more.",
                [PluginMessages.MessageMustLookAtDoor] = "You must look at the door of the room or put the roomId",
                [PluginMessages.GuiBoardAdmin] =
                    "\t\t\t\t\t<color=green>HOTEL MANAGER</color>\n\n<color=cyan>Hotel Name:\t\t{name}</color>\n<color=grey>Hotel Location:</color>\t{loc}\n<color=orange>Hotel Radius:\t\t{hrad}</color>\t<color=yellow>Rooms Radius:\t{rrad}</color>\n<color=blue>Rooms:\t{rnum}</color>\t\t<color=red>Occupied:\t{onum}</color>\t\t<color=green>Vacant:\t{fnum}</color>\n<color=cyan>Rent Price:\t{rp}\t</color><color=purple>{rc}</color>\n<color=grey>Duration:\t{rd} Seconds</color>\n<color=orange>Kick Hobos:\t{kh}</color>\t\t<color=yellow>NPC Id:\t{npcId}</color>\n<color=blue>Show Marker:\t{sm}</color>\n<color=red>Permission:\t{p}</color>",
                [PluginMessages.GuiBoardBlackList] = "<color=blue>You cannot enter {name} Hotel with any of the following items:</color>\r\n<size=10>{blacklist}</size>",
                [PluginMessages.GuiBoardPlayer] =
                    "\t\t\t\t\t\t\t\t\t\t<color=yellow><size=16>{name}</size></color>\n\t\t\t\t\t<color=yellow><size=12>Location: ({loc})</size></color>\n\t\t<color=blue>Rooms:\t\t{rnum}</color>\t\t<color=green>Price:\t{price} {currency} per {durHours} hours </color>\n\t\t<color=red>Occupied:\t{onum}</color>\n\t\t<color=green>Vacant:\t\t{fnum}</color>",
                [PluginMessages.GuiBoardPlayerRoom] =
                    "\n\t\t\t\t\t\t\t\t<color=yellow><size=14>Your Room</size></color>\n\t\t\t\t\t<color=blue><size=12>Id:\t\t\t\t{rid}</size></color>\n\t\t\t\t\t<color=orange><size=12>Joined:\t\t\t{jdate}</size></color>\n\t\t\t\t\t<color=cyan><size=12>Time Left:\t\t{timeleft}</size></color>",
                [PluginMessages.GuiBoardPlayerMaintenance] =
                    "\t\t\t\t\t\t\t\t\t\t<color=green>{name}</color>\n\nHotel is under maintenance. Please wait couple seconds/minutes until the admin is finished.",
                [PluginMessages.Menu1] = "<color=purple>==== Available options ====</color>",
                [PluginMessages.Menu2] =
                    "<color=green>/hotel location</color> => sets the center hotel location where you stand",
                [PluginMessages.Menu3] =
                    "<color=green>/hotel npc NpcId </color> => sets the NPC that is hooked to this hotel (for UseNPC items)",
                [PluginMessages.Menu4] =
                    "<color=green>/hotel permission \"Permission Name\"</color> => sets the oxide permissions that the player needs to rent a room here",
                [PluginMessages.Menu5] =
                    "<color=green>/hotel radius XX</color> => sets the radius of the hotel (the entire structure of the hotel needs to be covered by the zone)",
                [PluginMessages.Menu6] =
                    "<color=green>/hotel rentduration XX</color> => Sets the duration of a default rent in this hotel. 0 is infinite.",
                [PluginMessages.Menu7] =
                    "<color=green>/hotel rentcurrency XX</color> => (<color=orange>0</color> - Economics, <color=orange>1</color> - Server Rewards, \"<color=orange>short name</color>\" of item ie. scrap).",
                [PluginMessages.Menu8] = "<color=green>/hotel rentprice XX</color> => Sets the rental price of a room.",
                [PluginMessages.Menu9] =
                    "<color=green>/hotel kickhobos [true/false]</color> => Toggles kicking sleepers without rooms.",
                [PluginMessages.Menu10] =
                    "<color=green>/hotel showmarker [true/false]</color> => Toggles showing of map marker.",
                [PluginMessages.Menu11] =
                    "<color=green>/hotel reset</color> => resets the hotel data (all players and rooms, but keeps the hotel)",
                [PluginMessages.Menu12] = "<color=green>/hotel roomradius XX</color> => sets the radius of the rooms",
                [PluginMessages.Menu13] =
                    "<color=green>/hotel rooms</color> => refreshes the rooms (detects new rooms, deletes rooms if they don't exist anymore, if rooms are in use they won't get taken in count)",
                [PluginMessages.ExtendButtonText] = "Extend Your Stay"
            }, this);
        }

        private string GetMsg(string key, object userId = null)
        {
            return lang.GetMessage(key, this, userId?.ToString());
        }

    }
}