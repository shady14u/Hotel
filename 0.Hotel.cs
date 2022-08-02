//Requires: ZoneManager

//plugin.merge -c -m -p ./merge.json
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;


namespace Oxide.Plugins
{
    //Define:FileOrder=1
    [Info("Hotel", "Shady14u", "2.0.26")]
    [Description("Complete Hotel System for Rust.")]
    public partial class Hotel : RustPlugin
 
    {
        #region PluginReferences

        [PluginReference]
        Plugin ZoneManager, Economics, ServerRewards, Backpacks;

        #endregion

        #region Fields

        private Timer hotelGuiTimer;
        private Timer hotelRoomCheckoutTimer;
        private readonly Hash<BasePlayer, Timer> playerGuiTimers = new Hash<BasePlayer, Timer>();
        private readonly Hash<BasePlayer, Timer> playerBlackListGuiTimers = new Hash<BasePlayer, Timer>();
        private readonly Hash<ulong, HotelData> hotelGuests = new Hash<ulong, HotelData>();
        
        static readonly int ConstructionColl = LayerMask.GetMask("Construction", "Construction Trigger");
        static readonly int DeployableColl = LayerMask.GetMask("Deployed");
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        public static Quaternion DefaultQuaternion = new Quaternion(0f, 0f, 0f, 0f);
        private static Dictionary<string, HotelData> EditHotel = new Dictionary<string, HotelData>();
        private static Dictionary<string, HotelMarker> HotelMarkers = new Dictionary<string, HotelMarker>();
        private static readonly Vector3 Vector3Up = new Vector3(0f, 0.1f, 0f);
        private static readonly Vector3 Vector3Up2 = new Vector3(0f, 1.5f, 0f);


        #endregion
        

       
       
        #region Helper Methods

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
                var money = Convert.ToInt32(Economics.Call<double>("Balance", player.UserIDString));
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
                    (x.CheckOutTime() != 0.0 && x.CheckOutTime() < currentTime)))
                {
                    ResetRoom(room);
                }

                foreach (var room in hotel.rooms.Values.Where(x => string.IsNullOrEmpty(x.renter)))
                {
                    var codeLock = FindCodeLockByPos(room.Pos());
                    if (codeLock != null)
                    {
                        UnlockLock(codeLock);
                    }
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

        private IPlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in covalence.Players.All)
            {
                if (activePlayer.Id == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.Name.Contains(nameOrIdOrIp))
                    return activePlayer;
                if (activePlayer.Name.ToLower().Contains(nameOrIdOrIp.ToLower()))
                    return activePlayer;
                if (activePlayer.Address == nameOrIdOrIp)
                    return activePlayer;
            }

            return null;
        }

        private bool FindRoomById(string roomId, out HotelData targetHotel, out Room targetRoom)
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
            }

            return roomTimeMessage;
        }

        private bool HasBlackListedItems(BasePlayer player, List<string> blackList)
        {
            ItemContainer backpack = null;
            if (Backpacks && Backpacks.IsLoaded)
            {
                backpack = Backpacks.Call<ItemContainer>("API_GetBackpackContainer", player.userID);
            }
            foreach (var item in blackList)
            {
                if (player.inventory.GetAmount(int.Parse(item.Split('_')[0])) > 0)
                {
                    return true;
                };
                if (backpack != null && backpack.GetAmount(int.Parse(item.Split('_')[0]), false) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAccess(BasePlayer player, string accessRole = "admin")
        {
            if (player == null) return false;
            return player.net.connection.authLevel >= config.AuthLevel ||
                   permission.UserHasPermission(player.UserIDString, $"hotel.{accessRole}");
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
                            RemovePlayerFromZone(oldTenant);
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

        private void RemovePlayerFromZone(BasePlayer player)
        {
            var position = (player.transform.position.XZ3D() * 100f);

            RaycastHit rayHit;
            if (Physics.Raycast(new Ray(new Vector3(position.x, position.y + 300, position.z), Vector3.down), out rayHit, 500, ~(1 << 10 | 1 << 18 | 1 << 28 | 1 << 29), QueryTriggerInteraction.Ignore))
                position.y = rayHit.point.y;
            else position.y = TerrainMeta.HeightMap.GetHeight(position);
            
            player.MovePosition(position);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.SendNetworkUpdateImmediate();
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

        private void ResetRoom(Room room)
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

        

        private void CleanUpUi()
        {
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, "HotelTimer");
            }
        }

        private static void UnlockLock(CodeLock codeLock)
        {
            codeLock.SetFlag(BaseEntity.Flags.Locked, false);
            codeLock.SendNetworkUpdate();
        }

        private void UpdateHotelCounter()
        {
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

                if (roomTimeMessage.TimeMessage.StartsWith("No Room ") && config.HideUiForNonRenters)
                {
                    CuiHelper.DestroyUi(basePlayer, "HotelTimer");
                }else{
                    ShowHotelCounterUi(basePlayer, roomTimeMessage);
                }

            }
        }


        #endregion

     
    }
}
