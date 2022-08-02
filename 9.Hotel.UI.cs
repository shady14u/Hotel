using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class Hotel
    {
        #region GUI

        private void ShowHotelCounterUi(BasePlayer player, RoomTimeMessage roomTimeMessage)
        {
            var hotelCounterUi = new CuiElementContainer
            {
                {
                    new CuiLabel()
                    {
                        Text =
                        {
                            Text = roomTimeMessage.TimeMessage,
                            Align = TextAnchor.UpperLeft,
                            FontSize = config.CounterUiTextSize,
                            Color = roomTimeMessage.TimeRemaining < 600 ? "0.91 0.27 0.27 1" : config.CounterUiTextColor
                        },
                        RectTransform =
                        {
                            AnchorMin = config.CounterUiAnchorMin,
                            AnchorMax = config.CounterUiAnchorMax
                        }
                    },
                    "Hud", "HotelTimer"
                }
            };
            CuiHelper.DestroyUi(player, "HotelTimer");
            CuiHelper.AddUi(player, hotelCounterUi);
        }

        private void RefreshAdminHotelGui(BasePlayer player)
        {
            RemoveAdminHotelGui(player);

            if (!EditHotel.ContainsKey(player.UserIDString)) return;
            var msg = CreateAdminGuiMsg(player);
            if (msg == string.Empty) return;
            var send = config.AdminGuiJson.Replace("{msg}", msg);
            CuiHelper.AddUi(player, send);
        }

        private void RefreshBlackListGui(BasePlayer player, HotelData hotel, List<string> blackList)
        {
            RemoveBlackListGui(player);

            var msg = CreateBlackListGuiMsg(player, hotel, blackList);
            if (msg == string.Empty) return;
            var send = config.BlackListGuiJson.Replace("{msg}", msg);
            CuiHelper.AddUi(player, send);
            playerBlackListGuiTimers[player] = timer.Once(config.PanelTimeOut, () => RemoveBlackListGui(player));
        }

        private string CreateBlackListGuiMsg(BasePlayer player, HotelData hotel, List<string> blackList)
        {
            return GetMsg(PluginMessages.GuiBoardBlackList, player.UserIDString)
                .Replace("{name}", hotel.hotelName)
                .Replace("{blacklist}", string.Join("\r\n", blackList.Select(x => x.Split('_')[1]).OrderBy(x => x)));
        }

        private void RemoveBlackListGui(BasePlayer player)
        {
            if (player == null || player.net == null) return;
            if (playerBlackListGuiTimers[player] != null)
                playerBlackListGuiTimers[player].Destroy();

            CuiHelper.DestroyUi(player, "HotelBlackList");
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
                send = config.PlayerGuiJson.Replace("{msg}", msg);
                CuiHelper.AddUi(player, send);
            }
            else
            {
                msg = CreatePlayerGuiMsg(player, hotel, GetMsg(PluginMessages.GuiBoardPlayer, player.userID));
                if (msg == string.Empty) return;
                send = config.PlayerGuiJson.Replace("{msg}", msg);

                CuiHelper.AddUi(player, send);

                if (HasAccess(player, "extend") &&
                    hotel.rooms.Values.FirstOrDefault(x => x.renter == player.UserIDString) != null)
                {
                    //if Player can extend add button here
                    var extendContainer = new CuiElementContainer();
                    extendContainer.Add(new CuiButton
                    {
                        Button = {Color = ".3 .2 .3 1", Command = $"hotelextend '{hotel.hotelName}' ", FadeIn = 0.4f},
                        RectTransform = {AnchorMin = "0.7 0.8", AnchorMax = "1 1"},
                        Text =
                        {
                            Text = GetMsg(PluginMessages.ExtendButtonText, player.userID), FontSize = 12,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, "HotelPlayer");

                    CuiHelper.AddUi(player, extendContainer);
                }
            }

            playerGuiTimers[player] = timer.Once(config.PanelTimeOut, () => RemovePlayerHotelGui(player));
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
            var loc = hotel.x == null
                ? "None"
                : $"X : {double.Parse(hotel.x):F0}, Y : {double.Parse(hotel.y):F0}, Z : {double.Parse(hotel.z):F0}";
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
                    roomGui = GetMsg(PluginMessages.GuiBoardPlayerRoom, player.userID)
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
                                .Replace("{price}", hotel.e)
                                .Replace("{currency}",
                                    hotel.currency == "0" ? "Economics" :
                                    hotel.currency == "1" ? "Server Rewards" : hotel.currency)
                                .Replace("{durSeconds}", hotel.rd)
                                .Replace("{durHours}", (int.Parse(hotel.rd ?? "0") / 3600).ToString("F1"))
                                .Replace("{durDays}", (int.Parse(hotel.rd ?? "0") / 86400).ToString("F1"))
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
                return GetMsg(PluginMessages.GuiBoardAdmin, player.userID)
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

            return GetMsg(PluginMessages.GuiBoardAdmin, player.userID)
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
            CuiHelper.DestroyUi(player, "HotelAdmin");
        }

        private void RemovePlayerHotelGui(BasePlayer player)
        {
            if (player == null || player.net == null) return;
            if (playerGuiTimers[player] != null)
                playerGuiTimers[player].Destroy();

            CuiHelper.DestroyUi(player, "HotelPlayer");
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
                var markerMsg = config.MapMarker
                    .Replace("{name}", hotel.hotelName)
                    .Replace("{fnum}", hotel.rooms.Values.Count(x => x.renter == null).ToString())
                    .Replace("{onum}", hotel.rooms.Values.Count(x => x.renter != null).ToString())
                    .Replace("{rnum}", hotel.rooms.Values.Count.ToString())
                    .Replace("{rp}", hotel.e)
                    .Replace("{price}", hotel.e)
                    .Replace("{currency}",
                        hotel.currency == "0" ? "Economics" : hotel.currency == "1" ? "Server Rewards" : hotel.currency)
                    .Replace("{rc}",
                        hotel.currency == "0" ? "Economics" : hotel.currency == "1" ? "Server Rewards" : hotel.currency)
                    .Replace("{rd}", hotel.rd)
                    .Replace("{durHours}", (int.Parse(hotel.rd ?? "0") / 3600).ToString("F1"))
                    .Replace("{durDays}", (int.Parse(hotel.rd ?? "0") / 86400).ToString("F1"))
                    .Replace("{durSeconds}", hotel.rd);

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
            hotelMarker.GenericMapMarker.radius = Mathf.Min(2.5f, config.MapMarkerRadius);
            hotelMarker.GenericMapMarker.SendUpdate();
        }

        private Color GetMarkerColor(string id = "color1")
        {
            Color color;

            if (id == "color1")
            {
                return TryParseHtmlString(config.MapMarkerColor, out color) ? color : Color.magenta;
            }

            return TryParseHtmlString(config.MapMarkerColorBorder, out color) ? color : Color.magenta;
        }

        #endregion
    }
}