using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=80
    public partial class Hotel
    {
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

            public DeployableItem()
            {

            }

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

        public class HotelMarker
        {
            #region Properties and Indexers

            public MapMarkerGenericRadius GenericMapMarker { get; set; }
            public VendingMachineMapMarker VendingMachineMapMarker { get; set; }

            #endregion
        }
        

    }
}
