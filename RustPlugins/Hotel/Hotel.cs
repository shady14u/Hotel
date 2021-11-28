//Requires: ZoneManager

#region Using Statements

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

#endregion

namespace Oxide.Plugins
{
    [Info("Hotel", "Shady14u", "2.0.17")]
    [Description("Complete Hotel System for Rust.")]
    public class Hotel : RustPlugin
    {
        #region PluginReferences

        [PluginReference] 
        Plugin ZoneManager, Economics, ServerRewards, InfoPanel;

        #endregion

        #region Fields

        private Timer _hotelGuiTimer;
        private bool _hotelPanelLoaded;
        private Timer _hotelRoomCheckoutTimer;
        private readonly Hash<BasePlayer, Timer> _playerGuiTimers = new Hash<BasePlayer, Timer>();
        private readonly Hash<BasePlayer, Timer> _playerBlackListGuiTimers = new Hash<BasePlayer, Timer>();

        private static StoredData _storedData;
        
        static readonly int ConstructionColl = LayerMask.GetMask("Construction", "Construction Trigger");
        static readonly int DeployableColl = LayerMask.GetMask("Deployed");
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        
        public static Quaternion DefaultQuaternion = new Quaternion(0f, 0f, 0f, 0f);
        public static Dictionary<string, HotelData> EditHotel = new Dictionary<string, HotelData>();
        public static Dictionary<string, HotelMarker> HotelMarkers = new Dictionary<string, HotelMarker>(); 
        public static Vector3 Vector3Up = new Vector3(0f, 0.1f, 0f);
        public static Vector3 Vector3Up2 = new Vector3(0f, 1.5f, 0f);
        
        
        #endregion

        #region Config

        private static Configuration _config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "adminGuiJson")]
            public string AdminGuiJson;

            [JsonProperty(PropertyName = "authLevel")]
            public int AuthLevel;

            [JsonProperty(PropertyName = "enterZoneShowPlayerGUI")]
            public bool EnterZoneShowPlayerGui;

            [JsonProperty(PropertyName = "enterZoneShowRoom")]
            public bool EnterZoneShowRoom;

            [JsonProperty(PropertyName = "hotelPanel")]
            public HotelPanel HotelPanel;

            [JsonProperty(PropertyName = "KickHobos")]
            public bool KickHobos;

            [JsonProperty(PropertyName = "mapMarker")]
            public string MapMarker;

            [JsonProperty(PropertyName = "mapMarkerColor")]
            public string MapMarkerColor;

            [JsonProperty(PropertyName = "mapMarkerColorBorder")]
            public string MapMarkerColorBorder;

            [JsonProperty(PropertyName = "mapMarkerRadius")]
            public float MapMarkerRadius;

            [JsonProperty(PropertyName = "openDoorPlayerGUI")]
            public bool OpenDoorPlayerGui;

            [JsonProperty(PropertyName = "openDoorShowRoom")]
            public bool OpenDoorShowRoom;

            [JsonProperty(PropertyName = "panelTimeOut")]
            public int PanelTimeOut;

            [JsonProperty(PropertyName = "panelXMax")]
            public string PanelXMax;

            [JsonProperty(PropertyName = "panelXMin")]
            public string PanelXMin;

            [JsonProperty(PropertyName = "panelYMax")]
            public string PanelYMax;

            [JsonProperty(PropertyName = "panelYMin")]
            public string PanelYMin;

            [JsonProperty(PropertyName = "playerGuiJson")]
            public string PlayerGuiJson;

            [JsonProperty(PropertyName = "useNPCShowPlayerGUI")]
            public bool UseNpcShowPlayerGui;

            [JsonProperty(PropertyName = "useNPCShowRoom")]
            public bool UseNpcShowRoom;

            [JsonProperty(PropertyName = "xMax")] public string XMax;

            [JsonProperty(PropertyName = "xMin")] public string XMin;

            [JsonProperty(PropertyName = "yMax")] public string YMax;

            [JsonProperty(PropertyName = "yMin")] public string YMin;

            [JsonProperty(PropertyName = "blackListGuiJson")]
            public string BlackListGuiJson;

            [JsonProperty(PropertyName = "blackList")]
            public string[] BlackList;

            [JsonProperty(PropertyName = "defaultZoneFlags")]
            public string[] DefaultZoneFlags;

            #region Methods (Public)

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    OpenDoorPlayerGui = true,
                    OpenDoorShowRoom = false,
                    UseNpcShowPlayerGui = true,
                    UseNpcShowRoom = false,
                    EnterZoneShowPlayerGui = false,
                    EnterZoneShowRoom = false,
                    KickHobos = true,
                    XMin = "0.65",
                    XMax = "1.0",
                    YMin = "0.6",
                    YMax = "0.9",
                    PanelXMin = "0.3",
                    PanelXMax = "0.6",
                    PanelYMin = "0.7",
                    PanelYMax = "0.95",
                    PanelTimeOut = 10,
                    AuthLevel = 2,
                    HotelPanel = new HotelPanel
                    {
                        Autoload = true,
                        AnchorX = "Left",
                        AnchorY = "Bottom",
                        Available = true,
                        BackgroundColor = "0 0 0 0",
                        Dock = "TopLeftDock",
                        Width = 0.4,
                        Height = 0.95,
                        Margin = "0 0 0 0.01",
                        Order = 8,
                        Image = new HotelPanelImage
                        {
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Available = true,
                            BackgroundColor = "0 0 0 0",
                            Dock = "TopLeftDock",
                            Height = 0.8,
                            Margin = "0 0.05 0.1 0.05",
                            Order = 1,
                            Url = "https://i.imgur.com/XHm7WGb.png",
                            Width = 0.15
                        },
                        Text = new HotelPanelText
                        {
                            Align = "MiddleCenter",
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Available = true,
                            BackgroundColor = "0 0 0 0",
                            Dock = "TopLeftDock",
                            FontColor = "1 1 1 1",
                            FontSize = 10,
                            Content = "Hotel Rooms",
                            Height = 1,
                            Margin = "0 0.02 0 0 ",
                            Order = 2,
                            Width = 0.85
                        }
                    },
                    AdminGuiJson = @"[ {
                        ""name"": ""HotelAdmin"",
                        ""parent"": ""Overlay"",
                        ""components"":
                        [
                            {
                                 ""type"":""UnityEngine.UI.Image"",
                                 ""color"":""0.1 0.1 0.1 0.7"",
                            },
                            {
                                ""type"":""RectTransform"",
                                ""anchormin"": ""{xmin} {ymin}"",
                                ""anchormax"": ""{xmax} {ymax}""

                            }
                        ]
                    },
                    {
                        ""parent"": ""HotelAdmin"",
                        ""components"":
                        [
                            {
                                ""type"":""UnityEngine.UI.Text"",
                                ""text"":""{msg}"",
                                ""fontSize"":15,
                                ""align"": ""MiddleLeft"",
                            },
                            {
                                ""type"":""RectTransform"",
                                ""anchormin"": ""0.1 0.1"",
                                ""anchormax"": ""1 1""
                            }
                        ]
                    }]",
                    PlayerGuiJson = @"[
                    {
                        ""name"": ""HotelPlayer"",
                        ""parent"": ""Overlay"",
                        ""components"":
                        [
                            {
                                 ""type"":""UnityEngine.UI.Image"",
                                 ""color"":""0.1 0.1 0.1 0.7"",
                            },
                            {
                                ""type"":""RectTransform"",
                                ""anchormin"": ""{pxmin} {pymin}"",
                                ""anchormax"": ""{pxmax} {pymax}""

                            }
                        ]
                    },
                    {
                        ""parent"": ""HotelPlayer"",
                        ""components"":
                        [
                            {
                                ""type"":""UnityEngine.UI.Text"",
                                ""text"":""{msg}"",
                                ""fontSize"":15,
                                ""align"": ""MiddleLeft"",
                            },
                            {
                                ""type"":""RectTransform"",
                                ""anchormin"": ""0.1 0.1"",
                                ""anchormax"": ""1 1""
                            }
                        ]
                    }]",
                    BlackListGuiJson = @"[
                    {
                        ""name"": ""HotelBlackList"",
                        ""parent"": ""Overlay"",
                        ""components"":
                        [
                            {
                                 ""type"":""UnityEngine.UI.Image"",
                                 ""color"":""0.1 0.1 0.1 0.7"",
                            },
                            {
                                ""type"":""RectTransform"",
                                ""anchormin"": ""{pxmin} {pymin}"",
                                ""anchormax"": ""{pxmax} {pymax}""

                            }
                        ]
                    },
                    {
                        ""parent"": ""HotelBlackList"",
                        ""components"":
                        [
                            {
                                ""type"":""UnityEngine.UI.Text"",
                                ""text"":""{msg}"",
                                ""fontSize"":15,
                                ""align"": ""MiddleLeft"",
                            },
                            {
                                ""type"":""RectTransform"",
                                ""anchormin"": ""0.1 0.1"",
                                ""anchormax"": ""1 1""
                            }
                        ]
                    }]",
                    MapMarker = "\t\t\t{name} Hotel\r\n{fnum} of {rnum} Rooms Available\r\n{rp} {rc} per {rd} Seconds",
                    MapMarkerColor = "#710AC1",
                    MapMarkerColorBorder = "#5FCEA8",
                    MapMarkerRadius = 0.25f,
                    BlackList = new[]{"explosive.timed"},
                    DefaultZoneFlags = new []{"lootself","nobuild","nocup","nodecay","noentitypickup","noplayerloot","nostash","notrade","pvpgod","sleepgod","undestr"}
                };
            }

            #endregion
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception)
            {
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }

            if (_config != null)
            {
                _config.AdminGuiJson = _config.AdminGuiJson.Replace("{xmin}", _config.XMin)
                    .Replace("{xmax}", _config.XMax).Replace("{ymin}", _config.YMin).Replace("{ymax}", _config.YMax);
                _config.PlayerGuiJson = _config.PlayerGuiJson.Replace("{pxmin}", _config.PanelXMin)
                    .Replace("{pxmax}", _config.PanelXMax).Replace("{pymin}", _config.PanelYMin)
                    .Replace("{pymax}", _config.PanelYMax);
                _config.BlackListGuiJson = _config.BlackListGuiJson.Replace("{pxmin}", _config.PanelXMin)
                    .Replace("{pxmax}", _config.PanelXMax).Replace("{pymin}", _config.PanelYMin)
                    .Replace("{pymax}", _config.PanelYMax);
                
                var blackListTemp = new List<string>();

                foreach (var item in _config.BlackList)
                {
                    if (item.Contains("_"))
                    {
                        blackListTemp.Add(item);
                        continue;
                    }
                    int itemId;
                    var itemDefinition = int.TryParse(item, out itemId) ? ItemManager.FindItemDefinition(itemId) : ItemManager.FindItemDefinition(item);

                    if (itemDefinition == null) continue;
                    blackListTemp.Add($"{itemDefinition.itemid}_{itemDefinition.displayName.translated}");
                }

                _config.BlackList = blackListTemp.ToArray();
            }

            LoadData();
            LoadPermissions();
        }

        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        
        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Localization

        private static class PluginMessages
        {
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
        }

        private string GetMsg(string key, object userId = null)
        {
            return lang.GetMessage(key, this, userId?.ToString());
        }


        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
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
                [PluginMessages.MessageErrorAlreadyGotRoom] = "You already have a room in this hotel and !",
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
                [PluginMessages.GuiBoardBlackList] ="<color=blue>You cannot enter {name} Hotel with any of the following items:</color>\r\n<size=10>{blacklist}</size>",
                [PluginMessages.GuiBoardPlayer] =
                    "\t\t\t\t\t\t\t\t\t\t<color=yellow><size=16>{name}</size></color>\n\t\t\t\t\t<color=yellow><size=12>Location: ({loc})</size></color>\n\t\t\t\t\t\t<color=blue>Rooms:\t\t\t\t{rnum}</color>\n\t\t\t\t\t\t<color=red>Occupied:\t\t\t{onum}</color>\n\t\t\t\t\t\t<color=green>Vacant:\t\t\t\t{fnum}</color>",
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
                    "<color=green>/hotel rooms</color> => refreshes the rooms (detects new rooms, deletes rooms if they don't exist anymore, if rooms are in use they won't get taken in count)"
            }, this);
        }

        #endregion

        #region OxideHooks

        object CanPickupLock(BasePlayer player, BaseLock baseLock)
        {
            var codeLock = baseLock as CodeLock;
            var parentEntity = codeLock?.GetParentEntity();
            if (parentEntity == null || !parentEntity.name.Contains("door")) return null;

            var playersZones = ZoneManager.Call<string[]>("GetPlayerZoneIDs", player);

            if (_storedData.Hotels.Any(hotel => playersZones.Contains(hotel.hotelName)))
            {
                return false;
            }

            return null;
        }

        object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            var codeLock = baseLock as CodeLock;
            var parentEntity = codeLock?.GetParentEntity();
            if (parentEntity == null || !parentEntity.name.Contains("door")) return null;

            var playersZones = ZoneManager.Call<string[]>("GetPlayerZoneIDs", player);

            var targetHotel = _storedData.Hotels.FirstOrDefault(hotel => playersZones.Contains(hotel.hotelName));

            if (targetHotel == null) return null;

            if (_config.OpenDoorPlayerGui)
                RefreshPlayerHotelGui(player, targetHotel);
            if (_config.OpenDoorShowRoom)
                ShowPlayerRoom(player, targetHotel);

            if (!targetHotel.enabled)
            {
                SendReply(player, GetMsg("MessageMaintenance", player.userID));
                return false;
            }

            var room = FindRoomByDoorAndHotel(targetHotel, parentEntity);
            if (room == null)
            {
                SendReply(player, GetMsg("MessageErrorUnavailableRoom", player.userID));
                return false;
            }

            if (room.renter == null)
            {
                if (!CanRentRoom(player, targetHotel)) return false;

                NewRoomOwner(codeLock, player, targetHotel, room);

                if (targetHotel.currency == "0" && Economics)
                {
                    EconomicsWithdraw(player, targetHotel.Price());
                }

                if (targetHotel.currency == "1" && ServerRewards)
                {
                    ServerRewardsWithdraw(player, targetHotel.Price());
                }

                if (targetHotel.currency != "0" && targetHotel.currency != "1")
                {
                    var collect = Pool.GetList<Item>();
                    var itemDefinition = ItemManager.FindItemDefinition(targetHotel.currency);
                    if (itemDefinition != null)
                    {
                        player.inventory.Take(collect, itemDefinition.itemid, int.Parse(targetHotel.e));
                        player.Command("note.inv", itemDefinition.itemid, -1 * int.Parse(targetHotel.e));
                    }
                }

                CreateMapMarker(targetHotel);
            }

            LockLock(codeLock);

            if (room.renter != player.UserIDString)
            {
                SendReply(player, GetMsg("MessageErrorNotAllowedToEnter", player.userID));
                return false;
            }

            SaveData();
            return true;
        }
        
        private static void LoadData()
        {
            try
            {
                _storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Hotel");
            }
            catch
            {
                _storedData = new StoredData();
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnEnterZone(string zoneId, BasePlayer player)
        {
            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.hotelName != null)
                .Where(hotel => hotel.hotelName == zoneId))
            {
                //TODO: Let each Hotel blacklist items?
                //var blackList = hotel.BlackList;
                
                if (HasBlackListedItems(player, _config.BlackList.ToList()))
                {
                    var zone = ZoneManager.Call<ZoneManager.Zone>("GetZoneByID", hotel.hotelName);
                    ZoneManager.Call("EjectPlayer", player, zone);
                    RefreshBlackListGui(player, hotel, _config.BlackList.ToList());
                    return;
                }

                if (_config.EnterZoneShowPlayerGui)
                    RefreshPlayerHotelGui(player, hotel);
                if (_config.EnterZoneShowRoom)
                    ShowPlayerRoom(player, hotel);
            }
            
        }
        
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player == null)
                return;

            foreach (var hotel in from hotel in _storedData.Hotels
                                  where hotel.kickHobos
                                  let isInZone = ZoneManager.Call<bool>("IsPlayerInZone", hotel.hotelName, player)
                                  where isInZone
                                  select hotel)
            {
                if (hotel.rooms.Any(hotelRoom => hotelRoom.Value.renter == player.userID.ToString()))
                {
                    return;
                }

                //Player is in a hotel and they don't have a room.. Kick them.
                var zone = ZoneManager.Call<ZoneManager.Zone>("GetZoneByID", hotel.hotelName);
                ZoneManager.Call("EjectPlayer", player, zone);
                return;
            }
        }

        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Title == "InfoPanel")
            {
                InfoPanelInit();
            }
        }

        void OnServerInitialized(bool initial)
        {
            InfoPanelInit();
            CheckTimeOutRooms();
            _hotelRoomCheckoutTimer = timer.Repeat(60f, 0, CheckTimeOutRooms);
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            var npcId = npc.UserIDString;
            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.npc != null && hotel.npc == npcId))
            {
                if (_config.UseNpcShowPlayerGui)
                    RefreshPlayerHotelGui(player, hotel);
                if (_config.UseNpcShowRoom)
                    ShowPlayerRoom(player, hotel);
            }
        }

        #endregion

        #region Helper Methods

        private void AddHotelPanel()
        {
            try
            {
                _hotelPanelLoaded = InfoPanel.Call<bool>("PanelRegister", "Hotel", "HotelPanel",
                    JsonConvert.SerializeObject(_config.HotelPanel));
                if (_hotelPanelLoaded)
                    InfoPanel.Call("ShowPanel", "Hotel", "HotelPanel");
            }
            catch
            {
                Debug.LogWarning("Unable to create Hotel Panel. Is InfoPanel Installed?");
            }
        }

        private bool CanRentRoom(BasePlayer player, HotelData hotel, bool isExtending = false)
        {
            var playerHasRoom = false;
            if (hotel.rooms.Values.Any(room => room.renter == player.UserIDString))
            {
                if (!isExtending)
                {
                    SendReply(player, GetMsg(PluginMessages.MessageErrorAlreadyGotRoom, player.UserIDString));
                    return false;
                }

                playerHasRoom = true;
            }

            if (isExtending && !playerHasRoom)
            {
                SendReply(player, PluginMessages.MessageCouldNotFindToExtend.Replace("{0}", hotel.hotelName));
                return false;
            }

            if (hotel.p != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, $"hotel.{hotel.p.ToLower()}"))
                {
                    SendReply(player, GetMsg(PluginMessages.MessageErrorPermissionsNeeded, player.UserIDString)
                        .Replace("{0}", hotel.p));
                    return false;
                }
            }

            if (hotel.currency == "0" && Economics == null) return false;

            if (hotel.currency == "0")
            {
                var money = Convert.ToInt32((double)Economics.Call("Balance", player.UserIDString));
                if (money >= hotel.Price()) return true;
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotEnoughCoins, player.UserIDString)
                    .Replace("{0}", hotel.e)
                    .Replace("{1}", money.ToString()));
                return false;
            }

            if (hotel.currency == "1" && ServerRewards == null) return false;

            if (hotel.currency == "1")
            {
                var money = Convert.ToInt32(ServerRewards?.Call("CheckPoints", player.UserIDString));
                if (money >= hotel.Price()) return true;
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotEnoughRp, player.UserIDString)
                    .Replace("{0}", hotel.e)
                    .Replace("{1}", money.ToString()));
                return false;
            }

            var missingAmount = 0;
            var itemName = hotel.currency;
            var itemDefinition = ItemManager.FindItemDefinition(hotel.currency);
            if (itemDefinition != null)
            {
                missingAmount = int.Parse(hotel.e) - player.inventory.GetAmount(itemDefinition.itemid);
                itemName = itemDefinition.displayName.translated;
            }

            if (missingAmount <= 0) return true;
            SendReply(player, GetMsg(PluginMessages.MessageErrorNotEnoughItems, player.UserIDString)
                .Replace("{0}", itemName)
                .Replace("{1}", missingAmount.ToString()));
            return false;
        }

        void CheckTimeOutRooms()
        {
            var currentTime = LogTime();

            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.enabled))
            {
                foreach (var room in hotel.rooms.Values.Where(x =>
                    x.CheckOutTime() != 0.0 && x.CheckOutTime() < currentTime))
                {
                    ResetRoom(room);
                }

                CreateMapMarker(hotel);
            }
        }

        private void CleanUpMarkers()
        {
            foreach (var hotelMarker in HotelMarkers)
            {
                hotelMarker.Value.VendingMachineMapMarker?.Kill();
                hotelMarker.Value.GenericMapMarker?.Kill();
            }

        }

        private static void CloseDoor(Door door)
        {
            door.SetFlag(BaseEntity.Flags.Open, false);
            door.SendNetworkUpdateImmediate(true);
        }

        private void EconomicsWithdraw(BasePlayer player, int amount)
        {
            Economics?.Call("Withdraw", player.UserIDString, (double)amount);
            SendReply(player, $"You payed for this room {amount} coins");
        }

        private static void EmptyDeployablesRoom(BaseEntity door, float radius)
        {
            var foundItems = new List<BaseEntity>();
            var doorPos = door.transform.position;
            foreach (var col in Physics.OverlapSphere(doorPos, radius, DeployableColl))
            {
                var deploy = col.GetComponentInParent<BaseEntity>();
                if (deploy == null) continue;
                if (foundItems.Contains(deploy)) continue;

                var canReach = Physics.RaycastAll(deploy.transform.position + Vector3Up,
                        (doorPos + Vector3Up - deploy.transform.position).normalized,
                        Vector3.Distance(deploy.transform.position, doorPos) - 0.2f, ConstructionColl)
                    .All(rayHit =>
                        rayHit.collider.GetComponentInParent<Door>() != null &&
                        rayHit.collider.GetComponentInParent<Door>() == door);

                if (!canReach) continue;

                foreach (var col2 in Physics.OverlapSphere(doorPos, radius, ConstructionColl))
                {
                    BaseEntity door2 = col2.GetComponentInParent<Door>();
                    if (door2 == null) continue;
                    if (door2.transform.position == doorPos) continue;

                    var canReach2 = !Physics.RaycastAll(deploy.transform.position + Vector3Up,
                            (door2.transform.position + Vector3Up - deploy.transform.position).normalized,
                            Vector3.Distance(deploy.transform.position, door2.transform.position) - 0.2f,
                            ConstructionColl)
                        .Any();

                    if (!canReach2) continue;
                    canReach = false;
                    break;
                }

                if (!canReach) continue;

                foundItems.Add(deploy);
            }

            foreach (var deploy in foundItems.Where(deploy => deploy != null && !deploy.IsDestroyed))
            {
                deploy.KillMessage();
            }
        }

        private static Dictionary<string, Room> FindAllRooms(Vector3 position, float radius, float roomradius)
        {
            var listLocks = FindDoorsFromPosition(position, radius);

            var deployables = new Hash<BaseEntity, string>();
            var tempRooms = new Dictionary<string, Room>();

            foreach (var door in listLocks)
            {
                var pos = door.transform.position;
                var newRoom = new Room(pos)
                {
                    defaultDeployables = new List<DeployableItem>()
                };

                var foundItems = new List<BaseEntity>();

                foreach (var col in Physics.OverlapSphere(pos, roomradius, DeployableColl))
                {
                    var deploy = col.GetComponentInParent<BaseEntity>();
                    if (deploy == null) continue;
                    if (foundItems.Contains(deploy)) continue;
                    foundItems.Add(deploy);

                    var canReach = Physics.RaycastAll(deploy.transform.position + Vector3Up,
                            (pos + Vector3Up - deploy.transform.position).normalized,
                            Vector3.Distance(deploy.transform.position, pos) - 0.2f, ConstructionColl)
                        .All(rayHit =>
                            rayHit.collider.GetComponentInParent<Door>() != null &&
                            rayHit.collider.GetComponentInParent<Door>() == door);

                    if (!canReach) continue;

                    if (deployables[deploy] != null) deployables[deploy] = "0";
                    else deployables[deploy] = newRoom.roomId;
                }

                tempRooms.Add(newRoom.roomId, newRoom);
            }

            foreach (var pair in deployables)
            {
                if (pair.Value == "0") continue;
                var newDeployItem = new DeployableItem(pair.Key);
                tempRooms[pair.Value].defaultDeployables.Add(newDeployItem);
            }

            return tempRooms;
        }

        private static BuildingBlock FindBlockFromRay(Vector3 pos, Vector3 aim)
        {
            var hits = Physics.RaycastAll(pos, aim);
            var distance = 100000f;
            BuildingBlock target = null;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BuildingBlock>() == null || !(hit.distance < distance)) continue;
                distance = hit.distance;
                target = hit.collider.GetComponentInParent<BuildingBlock>();
            }

            return target;
        }

        private static CodeLock FindCodeLockByPos(Vector3 pos)
        {
            CodeLock findCode = null;
            foreach (var col in Physics.OverlapSphere(pos, 2f, ConstructionColl))
            {
                if (col.GetComponentInParent<Door>() == null) continue;
                if (!col.GetComponentInParent<Door>().HasSlot(BaseEntity.Slot.Lock)) continue;

                var slotEntity = col.GetComponentInParent<Door>().GetSlot(BaseEntity.Slot.Lock);
                if (slotEntity == null) continue;
                if (slotEntity.GetComponent<CodeLock>() == null) continue;

                if (findCode != null)
                    if (Vector3.Distance(pos, findCode.GetParentEntity().transform.position) <
                        Vector3.Distance(pos, col.transform.position))
                        continue;
                findCode = slotEntity.GetComponent<CodeLock>();
            }

            return findCode;
        }

        private static CodeLock FindCodeLockByRoomId(string roomId)
        {
            var roomPos = roomId.Split(':');
            return roomPos.Length != 3
                ? null
                : FindCodeLockByPos(new Vector3(Convert.ToSingle(roomPos[0]), Convert.ToSingle(roomPos[1]),
                    Convert.ToSingle(roomPos[2])));
        }

        private static List<Door> FindDoorsFromPosition(Vector3 position, float radius)
        {
            var listLocks = new List<Door>();
            foreach (var col in Physics.OverlapSphere(position, radius, ConstructionColl))
            {
                var door = col.GetComponentInParent<Door>();
                if (door == null) continue;
                if (!door.HasSlot(BaseEntity.Slot.Lock)) continue;
                if (door.GetSlot(BaseEntity.Slot.Lock) == null) continue;
                if (!(door.GetSlot(BaseEntity.Slot.Lock) is CodeLock)) continue;
                if (listLocks.Contains(door)) continue;
                CloseDoor(door);
                listLocks.Add(door);
            }

            return listLocks;
        }

        private bool FindHotelAndRoomByPos(Vector3 position, out HotelData hotelData, out Room roomData)
        {
            hotelData = null;
            roomData = null;
            position.x = Mathf.Ceil(position.x);
            position.y = Mathf.Ceil(position.y);
            position.z = Mathf.Ceil(position.z);

            foreach (var hotel in _storedData.Hotels)
            {
                foreach (var room in hotel.rooms.Values.Where(room => room.Pos() == position))
                {
                    hotelData = hotel;
                    roomData = room;
                    return true;
                }
            }

            return false;
        }

        private static bool FindRoomById(string roomId, out HotelData targetHotel, out Room targetRoom)
        {
            targetHotel = null;
            targetRoom = null;
            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.rooms.ContainsKey(roomId)))
            {
                targetHotel = hotel;
                targetRoom = (hotel.rooms)[roomId];
                return true;
            }

            return false;
        }

        private static Room FindRoomByDoorAndHotel(HotelData hotel, BaseEntity door)
        {
            var roomId =
                $"{Math.Ceiling(door.transform.position.x)}:{Math.Ceiling(door.transform.position.y)}:{Math.Ceiling(door.transform.position.z)}";
            return !hotel.rooms.ContainsKey(roomId) ? null : hotel.rooms[roomId];
        }

        private RoomTimeMessage GetRoomTimeLeft(HotelData hotel, string userIdString)
        {
            var roomTimeMessage = new RoomTimeMessage
            {
                TimeMessage = "No Room Rented.",
                TimeRemaining = double.MaxValue
            };

            foreach (var secondsLeft in from room in hotel.rooms.Values
                where room.renter == userIdString
                select room.CheckOutTime() - LogTime())
            {
                roomTimeMessage.TimeRemaining = secondsLeft;
                if (secondsLeft > 0)
                {
                    roomTimeMessage.TimeMessage =
                        ConvertSecondsToBetter(secondsLeft) + " (" + hotel.hotelName + " Expires)";
                }
                else
                {
                    roomTimeMessage.TimeMessage = "Your " + hotel.hotelName + " room expired";
                }

                if (secondsLeft < 600 && InfoPanel)
                {
                    InfoPanel.Call("SetPanelAttribute", "Hotel", "HotelPanelText", "FontColor", "0.6 0.1 0.1 1",
                        userIdString);
                }
                else
                {
                    InfoPanel.Call("SetPanelAttribute", "Hotel", "HotelPanelText", "FontColor", "0 1 0 1",
                        userIdString);
                }
            }

            return roomTimeMessage;
        }

        private bool HasBlackListedItems(BasePlayer player, List<string> blackList)
        {
            foreach (var item in blackList)
            {
                if (player.inventory.GetAmount(int.Parse(item.Split('_')[0])) > 0)
                {
                    return true;
                };
            }

            return false;
        }

        private void InfoPanelInit()
        {
            if (!InfoPanel || !InfoPanel.IsLoaded) return;

            InfoPanel.Call("SendPanelInfo", "Hotel", new List<string> { "HotelPanel" });
            AddHotelPanel();
            if (_hotelGuiTimer == null && _hotelPanelLoaded)
            {
                _hotelGuiTimer = timer.Repeat(5, 0, UpdateHotelCounter);
            }
        }

        
        private void LoadPermissions()
        {
            permission.RegisterPermission("hotel.admin", this);
            permission.RegisterPermission("hotel.extend", this);

            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.p != null))
            {
                permission.RegisterPermission("hotel." + hotel.p, this);
            }
        }

        private static void LockLock(CodeLock codeLock)
        {
            codeLock.SetFlag(BaseEntity.Flags.Locked, true);
            codeLock.SendNetworkUpdate();
        }
        
        static double LogTime()
        {
            return DateTime.UtcNow.Subtract(Epoch).TotalSeconds;
        }

        private void NewRoomOwner(CodeLock codeLock, BasePlayer player, HotelData hotel, Room room)
        {
            var door = codeLock.GetParentEntity();

            //Reset only if new renter
            if (string.IsNullOrEmpty(room.lastRenter) || room.lastRenter != player.UserIDString)
            {
                EmptyDeployablesRoom(door, Convert.ToSingle(hotel.rr));
                foreach (var deploy in room.defaultDeployables)
                {
                    SpawnDeployable(deploy.prefabName, deploy.Pos(), deploy.Rot(), player, deploy.skinId);
                }

                if (!string.IsNullOrEmpty(room.lastRenter))
                {
                    var oldTenant = BasePlayer.FindByID(ulong.Parse(room.lastRenter));
                    if (oldTenant != null)
                    {
                        //Kick the old tenant out?
                        var isInZone = ZoneManager.Call<bool>("IsPlayerInZone", hotel.hotelName, oldTenant);

                        if (isInZone)
                        {
                            //Player is in a hotel and they don't have a room.. Kick them.
                            var zone = ZoneManager.Call<ZoneManager.Zone>("GetZoneByID", hotel.hotelName);
                            ZoneManager.Call("EjectPlayer", oldTenant, zone);
                        }
                    }
                }
            }

            var whitelist = new List<ulong> { player.userID };
            codeLock.whitelistPlayers = whitelist;

            room.renter = player.UserIDString;
            room.lastRenter = player.UserIDString;
            room.checkingTime = LogTime().ToString(CultureInfo.InvariantCulture);

            room.checkoutTime = hotel.rd == "0"
                ? "0"
                : (LogTime() + double.Parse(hotel.rd)).ToString(CultureInfo.InvariantCulture);
            room.Reset();

            LockLock(codeLock);
            OpenDoor(door as Door);
            var message = hotel.rd == "0"
                ? GetMsg(PluginMessages.MessageRentUnlimited, player.userID)
                : GetMsg(PluginMessages.MessageRentTimeLeft, player.userID)
                    .Replace("{0}", ConvertSecondsToBetter(hotel.rd));

            SendReply(player, message);
        }

        private static void OpenDoor(Door door)
        {
            door.SetFlag(BaseEntity.Flags.Open, true);
            door.SendNetworkUpdateImmediate(true);
        }

        static object RaycastAll<T>(Ray ray) where T : BaseEntity
        {
            var hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            var distance = 100f;
            object target = null;
            foreach (var hit in hits)
            {
                var ent = hit.GetEntity();
                if (!(ent is T) || !(hit.distance < distance)) continue;
                target = ent;
                break;
            }

            return target;
        }

        private Vector3 RayForDoor(BasePlayer player)
        {
            var target = RaycastAll<BaseEntity>(player.eyes.HeadRay());
            if (target == null) return default(Vector3);
            return ((BaseEntity)target).transform.position;
        }

        private static void ResetRoom(Room room)
        {
            var codeLock = FindCodeLockByPos(room.Pos());
            if (codeLock == null) return;
            ResetRoom(codeLock, room);
        }

        private static void ResetRoom(CodeLock codeLock, Room room)
        {
            var door = codeLock.GetParentEntity();
            codeLock.whitelistPlayers = new List<ulong>();

            UnlockLock(codeLock);
            CloseDoor(door as Door);

            room.lastRenter = room.renter;
            room.renter = null;
            room.checkingTime = null;
            room.checkoutTime = null;
            room.Reset();
        }

        private static void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Hotel", _storedData);
        }

        private void ServerRewardsWithdraw(BasePlayer player, int amount)
        {
            ServerRewards?.Call("TakePoints", player.UserIDString, amount);
            SendReply(player, $"You payed for this room {amount} Reward Points");
        }

        private static void SpawnDeployable(string prefabName, Vector3 pos, Quaternion rot, BasePlayer player = null,
            ulong skinId = 0)
        {
            var newPrefab = GameManager.server.FindPrefab(prefabName);
            if (newPrefab == null) return;
            var entity = GameManager.server.CreateEntity(prefabName, pos, rot);
            if (entity == null) return;
            if (player != null)
            {
                entity.OwnerID = player.userID;
                if (entity is SleepingBag)
                {
                    ((SleepingBag)entity).deployerUserID = player.userID;
                }

                entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
            }

            if (skinId != 0)
            {
                entity.skinID = skinId;
            }

            entity.Spawn();
        }

        private bool TryParseHtmlString(string value, out Color color)
        {
            if (!value.StartsWith("#"))
            {
                value = $"#{value}";
            }

            return ColorUtility.TryParseHtmlString(value, out color);
        }

        void Unload()
        {
            CleanUpMarkers();

            SaveData();
            _hotelRoomCheckoutTimer.Destroy();
        }

        private static void UnlockLock(CodeLock codeLock)
        {
            codeLock.SetFlag(BaseEntity.Flags.Locked, false);
            codeLock.SendNetworkUpdate();
        }

        private void UpdateHotelCounter()
        {
            if (!InfoPanel || !InfoPanel.IsLoaded || !_hotelPanelLoaded) return;

            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                var roomTimeMessage = new RoomTimeMessage
                {
                    TimeMessage = "No Room Rented.",
                    TimeRemaining = double.MaxValue
                };

                foreach (var roomTime in _storedData.Hotels.Select(hotel =>
                        GetRoomTimeLeft(hotel, basePlayer.UserIDString))
                    .Where(roomTime => roomTime.TimeRemaining < roomTimeMessage.TimeRemaining))
                {
                    roomTimeMessage = roomTime;
                }

                InfoPanel.Call("SetPanelAttribute", "Hotel", "HotelPanelText", "Content", roomTimeMessage.TimeMessage,
                    basePlayer.UserIDString);
            }

            InfoPanel.Call("RefreshPanel", "Hotel", "HotelPanel");
        }

        #endregion

        #region GUI

        private void RefreshAdminHotelGui(BasePlayer player)
        {
            RemoveAdminHotelGui(player);

            if (!EditHotel.ContainsKey(player.UserIDString)) return;
            var msg = CreateAdminGuiMsg(player);
            if (msg == string.Empty) return;
            var send = _config.AdminGuiJson.Replace("{msg}", msg);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection },
                null, "AddUI", send);
        }

        private void RefreshBlackListGui(BasePlayer player,HotelData hotel, List<string> blackList)
        {
            RemoveBlackListGui(player);

            var msg = CreateBlackListGuiMsg(player,hotel, blackList);
            if (msg == string.Empty) return;
            var send = _config.BlackListGuiJson.Replace("{msg}", msg);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection },
                null, "AddUI", send);
            _playerBlackListGuiTimers[player] = timer.Once(_config.PanelTimeOut, () => RemoveBlackListGui(player));
        }

        private string CreateBlackListGuiMsg(BasePlayer player, HotelData hotel, List<string> blackList)
        {
            return GetMsg(PluginMessages.GuiBoardBlackList,player.UserIDString)
                .Replace("{name}",hotel.hotelName)
                .Replace("{blacklist}",string.Join("\r\n",blackList.Select(x=>x.Split('_')[1]).OrderBy(x=>x)));
        }

        private void RemoveBlackListGui(BasePlayer player)
        {
            Puts($"Removing BL GUI");
            if (player == null || player.net == null) return;
            if (_playerBlackListGuiTimers[player] != null)
                _playerBlackListGuiTimers[player].Destroy();
            
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection },
                null, "DestroyUI", "HotelBlackList");
        }

        private void RefreshPlayerHotelGui(BasePlayer player, HotelData hotel)
        {
            RemovePlayerHotelGui(player);
            string msg;
            string send;

            if (!hotel.enabled)
            {
                msg = CreatePlayerGuiMsg(player, hotel,
                    GetMsg(PluginMessages.GuiBoardPlayerMaintenance, player.userID));
                send = _config.PlayerGuiJson.Replace("{msg}", msg);
            }
            else
            {
                msg = CreatePlayerGuiMsg(player, hotel, GetMsg(PluginMessages.GuiBoardPlayer, player.userID));
                if (msg == string.Empty) return;
                send = _config.PlayerGuiJson.Replace("{msg}", msg);
            }

            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection },
                null, "AddUI", send);
            _playerGuiTimers[player] = timer.Once(_config.PanelTimeOut, () => RemovePlayerHotelGui(player));
        }

        private static string ConvertSecondsToBetter(string seconds)
        {
            return ConvertSecondsToBetter(double.Parse(seconds));
        }

        private static string ConvertSecondsToBetter(double seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return $"{t.Days:D2}d:{t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s";
        }

        private static string ConvertSecondsToDate(string seconds)
        {
            return ConvertSecondsToDate(double.Parse(seconds));
        }

        private static string ConvertSecondsToDate(double seconds)
        {
            return Epoch.AddSeconds(seconds).ToLocalTime().ToString(CultureInfo.InvariantCulture);
        }

        private string CreatePlayerGuiMsg(BasePlayer player, HotelData hotel, string guiMsg)
        {
            var loc = hotel.x == null ? "None" : $"{hotel.x} {hotel.y} {hotel.z}";
            var hotelRadius = hotel.r ?? "None";
            var roomRadius = hotel.rr ?? "None";
            var roomCount = hotel.rooms?.Count ?? 0;

            var occupiedCount = 0;
            var freeCount = 0;

            var roomGui = string.Empty;

            if (hotel.rooms != null)
            {
                var playersRoom = hotel.rooms.Values.FirstOrDefault(x => x.renter == player.UserIDString);

                if (playersRoom != null)
                {
                    roomGui = GetMsg("GUIBoardPlayerRoom", player.userID)
                        .Replace("{jdate}", ConvertSecondsToDate(playersRoom.checkingTime))
                        .Replace("{rid}", playersRoom.roomId)
                        .Replace("{timeleft}",
                            playersRoom.CheckOutTime() == 0.0
                                ? "Unlimited"
                                : ConvertSecondsToBetter(playersRoom.CheckOutTime() - LogTime()));
                }

                occupiedCount = hotel.rooms.Values.Count(x => x.renter != null);
                freeCount = hotel.rooms.Values.Count(x => x.renter == null);
            }

            var newGuiMsg = guiMsg
                                .Replace("{name}", hotel.hotelName)
                                .Replace("{loc}", loc)
                                .Replace("{hrad}", hotelRadius)
                                .Replace("{rrad}", roomRadius)
                                .Replace("{rnum}", roomCount.ToString())
                                .Replace("{onum}", occupiedCount.ToString())
                                .Replace("{fnum}", freeCount.ToString())
                            + roomGui;

            return newGuiMsg;
        }

        private string CreateAdminGuiMsg(BasePlayer player)
        {
            var hotelData = EditHotel[player.UserIDString];

            var loc = hotelData.x == null ? "None" : $"{hotelData.x} {hotelData.y} {hotelData.z}";
            var hotelRadius = hotelData.r ?? "None";
            var roomRadius = hotelData.rr ?? "None";
            var rrp = hotelData.e ?? "None";
            var numberRooms = hotelData.rooms?.Count ?? 0;

            if (numberRooms == 0)
            {
                return GetMsg("GUIBoardAdmin", player.userID)
                    .Replace("{name}", hotelData.hotelName)
                    .Replace("{loc}", loc)
                    .Replace("{hrad}", hotelRadius)
                    .Replace("{rrad}", roomRadius)
                    .Replace("{rnum}", "0")
                    .Replace("{onum}", "0")
                    .Replace("{fnum}", "0")
                    .Replace("{npcId}", hotelData.npc)
                    .Replace("{kh}", hotelData.kickHobos ? "True" : "False")
                    .Replace("{sm}", hotelData.showMarker ? "True" : "False")
                    .Replace("{rd}", hotelData.rd)
                    .Replace("{p}", hotelData.p)
                    .Replace("{rc}",
                        hotelData.currency == "0" ? "Economics" :
                        hotelData.currency == "1" ? "Server Rewards" : hotelData.currency)
                    .Replace("{rp}", rrp);
            }

            var occupiedCount = hotelData.rooms?.Values.Count(x => x.renter != null);
            var freeCount = hotelData.rooms?.Values.Count(x => x.renter == null);

            return GetMsg("GUIBoardAdmin", player.userID)
                .Replace("{name}", hotelData.hotelName)
                .Replace("{loc}", loc)
                .Replace("{hrad}", hotelRadius)
                .Replace("{rrad}", roomRadius)
                .Replace("{rnum}", numberRooms.ToString())
                .Replace("{onum}", occupiedCount.ToString())
                .Replace("{fnum}", freeCount.ToString())
                .Replace("{npcId}", hotelData.npc)
                .Replace("{rd}", hotelData.rd)
                .Replace("{p}", hotelData.p)
                .Replace("{kh}", hotelData.kickHobos ? "True" : "False")
                .Replace("{sm}", hotelData.showMarker ? "True" : "False")
                .Replace("{rc}",
                    hotelData.currency == "0" ? "Economics" :
                    hotelData.currency == "1" ? "Server Rewards" : hotelData.currency)
                .Replace("{rp}", rrp);
        }

        private void RemoveAdminHotelGui(BasePlayer player)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection },
                null, "DestroyUI", "HotelAdmin");
        }

        private void RemovePlayerHotelGui(BasePlayer player)
        {
            if (player == null || player.net == null) return;
            if (_playerGuiTimers[player] != null)
                _playerGuiTimers[player].Destroy();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection },
                null, "DestroyUI", "HotelPlayer");
        }

        private void ShowHotelGrid(BasePlayer player)
        {
            var hotelData = EditHotel[player.UserIDString];
            if (hotelData.x != null && hotelData.r != null)
            {
                var hotelPos = hotelData.Pos();
                var hotelRadius = Convert.ToSingle(hotelData.r);
                player.SendConsoleCommand("ddraw.sphere", 5f, Color.blue, hotelPos, hotelRadius);
            }

            if (hotelData.rooms == null) return;
            foreach (var room in hotelData.rooms.Values)
            {
                var deployables = room.defaultDeployables;
                foreach (var deployable in deployables)
                {
                    player.SendConsoleCommand("ddraw.arrow", 10f, Color.green, room.Pos(), deployable.Pos(), 0.5f);
                }
            }
        }

        private static void ShowPlayerRoom(BasePlayer player, HotelData hotel)
        {
            var foundRoom = (from pair in hotel.rooms where pair.Value.renter == player.UserIDString select pair.Value)
                .FirstOrDefault();
            if (foundRoom == null) return;
            player.SendConsoleCommand("ddraw.arrow", 10f, Color.green, player.transform.position,
                foundRoom.Pos() + Vector3Up2, 0.5f);
        }

        public void CreateMapMarker(HotelData hotel)
        {
            HotelMarker hotelMarker;
            if (!hotel.showMarker)
            {
                if (!HotelMarkers.TryGetValue(hotel.hotelName, out hotelMarker)) return;
                
                hotelMarker.VendingMachineMapMarker?.Kill();
                hotelMarker.GenericMapMarker?.Kill();
                HotelMarkers.Remove(hotel.hotelName);
                return;
            }

            if (!HotelMarkers.TryGetValue(hotel.hotelName, out hotelMarker))
            {
                const uint marker = 3459945130;
                const uint radiusMarker = 2849728229;
                hotelMarker = new HotelMarker
                {
                    VendingMachineMapMarker =
                        GameManager.server.CreateEntity(StringPool.Get(marker), hotel.Pos()) as VendingMachineMapMarker,
                    GenericMapMarker =
                        GameManager.server.CreateEntity(StringPool.Get(radiusMarker), hotel.Pos()) as
                            MapMarkerGenericRadius
                };

                if (hotelMarker.VendingMachineMapMarker != null)
                {
                    hotelMarker.VendingMachineMapMarker.Spawn();
                }

                if (hotelMarker.GenericMapMarker != null)
                {
                    hotelMarker.GenericMapMarker.Spawn();
                }

                HotelMarkers.Add(hotel.hotelName, hotelMarker);
            }

            if (hotelMarker.VendingMachineMapMarker != null)
            {
                hotelMarker.VendingMachineMapMarker.server_vendingMachine = null;
                var roomCount = hotel.rooms.Count;
                var rentedRooms = hotel.rooms.Values.Count(x => !string.IsNullOrEmpty(x.renter));

                if (roomCount > rentedRooms)
                {
                    //Open Rooms make Icon Green
                    hotelMarker.VendingMachineMapMarker.SetFlag(BaseEntity.Flags.Busy, true);
                }

                hotelMarker.VendingMachineMapMarker.enabled = true;
                var markerMsg = _config.MapMarker
                    .Replace("{name}", hotel.hotelName)
                    .Replace("{fnum}", hotel.rooms.Values.Count(x => x.renter == null).ToString())
                    .Replace("{rnum}", hotel.rooms.Values.Count.ToString())
                    .Replace("{rp}", hotel.e)
                    .Replace("{rc}",
                        hotel.currency == "0" ? "Economics" : hotel.currency == "1" ? "Server Rewards" : hotel.currency)
                    .Replace("{rd}", hotel.rd);

                if (!string.IsNullOrEmpty(hotel.p))
                {
                    markerMsg += $"\r\n\"{hotel.p}\" Only";
                }

                hotelMarker.VendingMachineMapMarker.markerShopName = markerMsg;
                hotelMarker.VendingMachineMapMarker.SendNetworkUpdate();
            }

            if (hotelMarker.GenericMapMarker == null) return;
            hotelMarker.GenericMapMarker.alpha = 0.75f;
            hotelMarker.GenericMapMarker.color1 = GetMarkerColor();
            hotelMarker.GenericMapMarker.color2 = GetMarkerColor("color2");
            hotelMarker.GenericMapMarker.radius = Mathf.Min(2.5f, _config.MapMarkerRadius);
            hotelMarker.GenericMapMarker.SendUpdate();
        }

        private Color GetMarkerColor(string id = "color1")
        {
            Color color;

            if (id == "color1")
            {
                return TryParseHtmlString(_config.MapMarkerColor, out color) ? color : Color.magenta;
            }

            return TryParseHtmlString(_config.MapMarkerColorBorder, out color) ? color : Color.magenta;
        }

        #endregion

        #region Chat Commands

        private bool HasAccess(BasePlayer player, string accessRole = "admin")
        {
            if (player == null) return false;
            return player.net.connection.authLevel >= _config.AuthLevel ||
                   permission.UserHasPermission(player.UserIDString, $"hotel.{accessRole}");
        }

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
                SendReply(player, GetMsg("MessageErrorNotAllowed", player.userID));
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
            Puts("In Hotel Command");
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg("MessageErrorNotAllowed", player.userID));
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

                        var defaultZoneFlags = _config.DefaultZoneFlags;
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
                SendReply(player, GetMsg("MessageErrorNotAllowed", player.userID));
                return;
            }

            SendReply(player, "======= Hotel List ======");
            foreach (HotelData hotel in _storedData.Hotels)
            {
                SendReply(player, $"{hotel.hotelName} - {hotel.rooms.Count}");
            }
        }

        [ChatCommand("hotel_edit")]
        void CmdChatHotelEdit(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                SendReply(player, GetMsg("MessageErrorNotAllowed", player.userID));
                return;
            }

            if (EditHotel.ContainsKey(player.UserIDString))
            {
                SendReply(player, GetMsg("MessageAlreadyEditing", player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, GetMsg("MessageHotelEditHelp", player.userID));
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

        #region Extra Classes

        public class RoomTimeMessage
        {
            #region Properties and Indexers

            public HotelData HotelData { get; set; }
            public string TimeMessage { get; set; }
            public double TimeRemaining { get; set; }

            #endregion
        }

        public class HotelData
        {
            Vector3 _pos;
            public string currency;
            public string e;
            public bool enabled;
            public string hotelName;
            public bool kickHobos;
            public string markerColor;
            public string npc;
            public string p;
            public int price;
            public string r;
            public string rd;

            public Dictionary<string, Room> rooms;
            public string rr;
            public bool showMarker;
            public string x;
            public string y;
            public string z;

            #region Constructors

            public HotelData()
            {
                enabled = false;
                rooms = new Dictionary<string, Room>();
            }

            public HotelData(string hotelName)
            {
                this.hotelName = hotelName;
                x = "0";
                y = "0";
                z = "0";
                r = "60";
                rr = "10";
                rd = "86400";
                p = null;
                e = null;
                currency = "scrap";
                rooms = new Dictionary<string, Room>();
                enabled = false;
                kickHobos = true;
                markerColor = "1 1 1 1";
            }

            #endregion

            #region Methods (Public)

            public void Activate()
            {
                enabled = true;
            }

            public void AddRoom(Room newRoom)
            {
                if (rooms.ContainsKey(newRoom.roomId))
                    rooms.Remove(newRoom.roomId);

                rooms.Add(newRoom.roomId, newRoom);
            }

            public void Deactivate()
            {
                enabled = false;
            }

            public Vector3 Pos()
            {
                if (x == "0" && y == "0" && z == "0")
                    return default(Vector3);
                if (_pos == default(Vector3))
                    _pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return _pos;
            }

            public int Price()
            {
                return e == null ? 0 : Convert.ToInt32(e);
            }

            public void RefreshRooms()
            {
                if (Pos() == default(Vector3))
                    return;
                var detectedRooms = FindAllRooms(Pos(), Convert.ToSingle(r), Convert.ToSingle(rr));

                var toAdd = new List<string>();
                var toDelete = new List<string>();
                if (rooms == null) rooms = new Dictionary<string, Room>();
                if (rooms.Count > 0)
                {
                    foreach (var pair in rooms)
                    {
                        if (pair.Value.renter != null)
                        {
                            detectedRooms.Remove(pair.Key);
                            Debug.Log($"[Hotel] {pair.Key} is occupied and can't be edited");
                            continue;
                        }

                        if (!detectedRooms.ContainsKey(pair.Key))
                        {
                            toDelete.Add(pair.Key);
                        }
                    }
                }

                foreach (var pair in detectedRooms)
                {
                    if (!rooms.ContainsKey(pair.Key))
                    {
                        toAdd.Add(pair.Key);
                    }
                    else
                    {
                        rooms[pair.Key] = pair.Value;
                    }
                }

                foreach (var roomId in toDelete)
                {
                    rooms.Remove(roomId);
                    Debug.Log($"[Hotel] {roomId} doesn't exist anymore, removing this room");
                }

                foreach (var roomId in toAdd)
                {
                    Debug.Log($"[Hotel] {roomId} is a new room, adding it");
                    rooms.Add(roomId, detectedRooms[roomId]);
                }
            }

            #endregion
        }

        public class Room
        {
            public string checkingTime;
            public string checkoutTime;

            public List<DeployableItem> defaultDeployables;

            public double intCheckoutTime;

            public string lastRenter;
            public Vector3 pos;
            public string renter;
            public string roomId;
            public string x;
            public string y;
            public string z;

            #region Constructors
            
            public Room(Vector3 position)
            {
                x = Math.Ceiling(position.x).ToString(CultureInfo.InvariantCulture);
                y = Math.Ceiling(position.y).ToString(CultureInfo.InvariantCulture);
                z = Math.Ceiling(position.z).ToString(CultureInfo.InvariantCulture);
                roomId = $"{x}:{y}:{z}";
            }

            #endregion

            #region Methods (Public)

            public double CheckOutTime()
            {
                if (intCheckoutTime == default(double))
                    intCheckoutTime = Convert.ToDouble(checkoutTime);
                return intCheckoutTime;
            }

            public void ExtendDuration(double duration)
            {
                intCheckoutTime = intCheckoutTime + duration;
                checkoutTime = intCheckoutTime.ToString(CultureInfo.InvariantCulture);
            }

            public Vector3 Pos()
            {
                if (pos == default(Vector3))
                    pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return pos;
            }

            public void Reset()
            {
                intCheckoutTime = default(double);
            }

            #endregion
        }

        public class DeployableItem
        {
            Vector3 _pos;
            Quaternion _rot;
            public string prefabName;
            public string rw;
            public string rx;
            public string ry;
            public string rz;
            public ulong skinId;
            public string x;
            public string y;
            public string z;

            #region Constructors

            public DeployableItem(BaseEntity deployable)
            {
                prefabName = StringPool.Get(deployable.prefabID);

                x = deployable.transform.position.x.ToString(CultureInfo.InvariantCulture);
                y = deployable.transform.position.y.ToString(CultureInfo.InvariantCulture);
                z = deployable.transform.position.z.ToString(CultureInfo.InvariantCulture);

                rx = deployable.transform.rotation.x.ToString(CultureInfo.InvariantCulture);
                ry = deployable.transform.rotation.y.ToString(CultureInfo.InvariantCulture);
                rz = deployable.transform.rotation.z.ToString(CultureInfo.InvariantCulture);
                rw = deployable.transform.rotation.w.ToString(CultureInfo.InvariantCulture);

                skinId = deployable.skinID;
            }

            #endregion

            #region Methods (Public)

            public Vector3 Pos()
            {
                if (_pos == default(Vector3))
                    _pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return _pos;
            }

            public Quaternion Rot()
            {
                if (_rot.w == 0f)
                    _rot = new Quaternion(float.Parse(rx), float.Parse(ry), float.Parse(rz), float.Parse(rw));
                return _rot;
            }

            #endregion
        }

        class StoredData
        {
            #region Constructors

            public StoredData()
            {
                Hotels = new HashSet<HotelData>();
            }

            #endregion

            #region Properties and Indexers

            public HashSet<HotelData> Hotels { get; set; }

            #endregion
        }

        public class HotelPanelImage
        {
            #region Properties and Indexers

            public string AnchorX { get; set; }
            public string AnchorY { get; set; }
            public bool Available { get; set; }
            public string BackgroundColor { get; set; }
            public string Dock { get; set; }
            public double Height { get; set; }
            public string Margin { get; set; }
            public int Order { get; set; }
            public string Url { get; set; }
            public double Width { get; set; }

            #endregion
        }

        public class HotelPanelText
        {
            #region Properties and Indexers

            public string Align { get; set; }
            public string AnchorX { get; set; }
            public string AnchorY { get; set; }
            public bool Available { get; set; }
            public string BackgroundColor { get; set; }
            public string Content { get; set; }
            public string Dock { get; set; }
            public string FontColor { get; set; }
            public int FontSize { get; set; }
            public double Height { get; set; }
            public string Margin { get; set; }
            public int Order { get; set; }
            public double Width { get; set; }

            #endregion
        }

        public class HotelPanel
        {
            #region Properties and Indexers

            public string AnchorX { get; set; }
            public string AnchorY { get; set; }

            public bool Autoload { get; set; }
            public bool Available { get; set; }
            public string BackgroundColor { get; set; }
            public string Dock { get; set; }
            public double Height { get; set; }
            public HotelPanelImage Image { get; set; }
            public string Margin { get; set; }
            public int Order { get; set; }
            public HotelPanelText Text { get; set; }
            public double Width { get; set; }

            #endregion
        }

        public class HotelUIComponent
        {
            #region Properties and Indexers

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string align { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string anchormax { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string anchormin { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string color { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? fontSize { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string text { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string type { get; set; }

            #endregion
        }

        public class HotelUIPanel
        {
            #region Properties and Indexers

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public HotelUIComponent[] components { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string name { get; set; }

            [DefaultValue("")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string parent { get; set; }

            #endregion
        }

        public class HotelMarker
        {
            #region Properties and Indexers

            public MapMarkerGenericRadius GenericMapMarker { get; set; }
            public VendingMachineMapMarker VendingMachineMapMarker { get; set; }

            #endregion
        }

        #endregion

    }
}