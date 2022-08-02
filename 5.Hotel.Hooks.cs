//Reference:ZoneManager

using System;
using System.Linq;
using System.Security.Policy;
using Facepunch;


namespace Oxide.Plugins
{
    //Define:FileOrder=60
    public partial class Hotel
    {
        private void Init()
        {
            AddCovalenceCommand("hotelextend", "HotelExtend");
        }

        private void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            CleanUpMarkers();
            CleanUpUi();

            SaveData();
            hotelRoomCheckoutTimer?.Destroy();
            hotelGuiTimer?.Destroy();
        }

        #region OxideHooks

        object CanPickupLock(BasePlayer player, BaseLock baseLock)
        {
            var codeLock = baseLock as CodeLock;
            var parentEntity = codeLock?.GetParentEntity();
            if (parentEntity == null || !parentEntity.name.Contains("door")) return null;

            if (hotelGuests.Any(x => x.Key == player.userID))
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

            if (!_storedData.Hotels.Any(hotel => playersZones.Contains(hotel.hotelName)))
            {
                return null;
            }

            var targetHotel = _storedData.Hotels.FirstOrDefault(hotel => playersZones.Contains(hotel.hotelName));
            if (targetHotel == null) return null;

            if (config.OpenDoorPlayerGui)
                RefreshPlayerHotelGui(player, targetHotel);
            if (config.OpenDoorShowRoom)
                ShowPlayerRoom(player, targetHotel);

            if (!targetHotel.enabled)
            {
                SendReply(player, GetMsg(PluginMessages.MessageMaintenance, player.userID));
                return false;
            }

            var room = FindRoomByDoorAndHotel(targetHotel, parentEntity);
            if (room == null)
            {
                SendReply(player, GetMsg(PluginMessages.MessageErrorUnavailableRoom, player.userID));
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
                SendReply(player, GetMsg(PluginMessages.MessageErrorNotAllowedToEnter, player.userID));
                return false;
            }

            SaveData();
            return true;
        }
        
        

        void OnEnterZone(string zoneId, BasePlayer player)
        {
            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.hotelName != null)
                .Where(hotel => hotel.hotelName == zoneId))
            {
                //Log in our guest
                hotelGuests.Add(player.userID, hotel);
                //TODO: Let each Hotel blacklist items?
                //var blackList = hotel.BlackList;

                if (HasBlackListedItems(player, config.BlackList.ToList()))
                {
                    var zone = ZoneManager.Call("GetZoneByID", hotel.hotelName);
                    ZoneManager.Call("EjectPlayer", player, zone);
                    RefreshBlackListGui(player, hotel, config.BlackList.ToList());
                    return;
                }

                if (config.EnterZoneShowPlayerGui)
                    RefreshPlayerHotelGui(player, hotel);
                if (config.EnterZoneShowRoom)
                    ShowPlayerRoom(player, hotel);
            }
        }

        void OnExitZone(string ZoneID, BasePlayer player) // Called when a player leaves a zone
        {
            hotelGuests.Remove(player.userID);
        }

        object OnItemPickup(Item item, BasePlayer player)
        {
            HotelData hotel;
            if (!hotelGuests.TryGetValue(player.userID, out hotel)) return null;
            if (config.BlackList.Any(x => x.Split('_')[0] == item.info.itemid.ToString() || x.Split('_')[1] == item.info.displayName.translated) || HasBlackListedItems(player, config.BlackList.ToList()))
            {
                var zone = ZoneManager.Call("GetZoneByID", hotel.hotelName);
                ZoneManager.Call("EjectPlayer", player, zone);
                RefreshBlackListGui(player, hotel, config.BlackList.ToList());

            }
            return null;
        }

        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            HotelData hotel;
            if (!hotelGuests.TryGetValue(inventory.baseEntity.userID, out hotel)) return;
            if (HasBlackListedItems(inventory.baseEntity, config.BlackList.ToList()))
            {
                var zone = ZoneManager.Call("GetZoneByID", hotel.hotelName);
                ZoneManager.Call("EjectPlayer", inventory.baseEntity, zone);
                RefreshBlackListGui(inventory.baseEntity, hotel, config.BlackList.ToList());
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
                var zone = ZoneManager.Call("GetZoneByID", hotel.hotelName);
                ZoneManager.Call("EjectPlayer", player, zone);
                return;
            }
        }

        void OnServerInitialized(bool initial)
        {
            CheckTimeOutRooms();
            if (config.ShowRoomCounterUi)
            {
                hotelGuiTimer = timer.Repeat(5f, 0, UpdateHotelCounter);
            }

            hotelRoomCheckoutTimer = timer.Repeat(60f, 0, CheckTimeOutRooms);
        }


        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            var npcId = npc.UserIDString;
            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.npc != null && hotel.npc == npcId))
            {
                if (config.UseNpcShowPlayerGui)
                    RefreshPlayerHotelGui(player, hotel);
                if (config.UseNpcShowRoom)
                    ShowPlayerRoom(player, hotel);
            }
        }

        #endregion


    }
}
