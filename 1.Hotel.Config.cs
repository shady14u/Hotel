using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=20
    public partial class Hotel
    {
        private static Configuration config;

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

            [JsonProperty(PropertyName = "showRoomCounterUi")]
            public bool ShowRoomCounterUi { get; set; } = false;

            [JsonProperty(PropertyName = "counterUiAnchorMin")]
            public string CounterUiAnchorMin { get; set; } = "0.11 0.9";

            [JsonProperty(PropertyName = "counterUiAnchorMax")]
            public string CounterUiAnchorMax { get; set; } = "0.28 0.98";

            [JsonProperty(PropertyName = "counterUiTextColor")]
            public string CounterUiTextColor { get; set; } = "0 1 0 1";
            [JsonProperty(PropertyName = "counterUiTextSize")]
            public int CounterUiTextSize { get; set; } = 12;
            [JsonProperty(PropertyName = "hideUiForNonRenters")]
            public bool HideUiForNonRenters { get; set; } = true;


            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    OpenDoorPlayerGui = true,
                    OpenDoorShowRoom = false,
                    UseNpcShowPlayerGui = true,
                    UseNpcShowRoom = true,
                    EnterZoneShowPlayerGui = false,
                    EnterZoneShowRoom = true,
                    KickHobos = true,
                    ShowRoomCounterUi = true,
                    XMin = "0.65",
                    XMax = "1.0",
                    YMin = "0.6",
                    YMax = "0.9",
                    PanelXMin = "0.3",
                    PanelXMax = "0.6",
                    PanelYMin = "0.6",
                    PanelYMax = "0.95",
                    PanelTimeOut = 10,
                    AuthLevel = 2,
                    AdminGuiJson = @"[{""name"": ""HotelAdmin"",""parent"": ""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0.1 0.1 0.1 0.7"",},
                            {""type"":""RectTransform"",""anchormin"": ""{xmin} {ymin}"", ""anchormax"": ""{xmax} {ymax}""}]},{""parent"": ""HotelAdmin"",""components"":
                            [{""type"":""UnityEngine.UI.Text"", ""text"":""{msg}"",""fontSize"":15, ""align"": ""MiddleLeft""},{""type"":""RectTransform"",
                            ""anchormin"": ""0.1 0.1"",""anchormax"": ""1 1"" }]}]",
                    PlayerGuiJson = @"[{""name"": ""HotelPlayer"",""parent"": ""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0.1 0.1 0.1 0.7"",},
                            {""type"":""RectTransform"",""anchormin"": ""{pxmin} {pymin}"",""anchormax"": ""{pxmax} {pymax}""}]},{""parent"": ""HotelPlayer"",""components"":
                            [{""type"":""UnityEngine.UI.Text"",""text"":""{msg}"",""fontSize"":15,""align"": ""MiddleLeft"",},{""type"":""RectTransform"",
                            ""anchormin"": ""0.1 0.1"",""anchormax"": ""1 0.8""}]}]",
                    BlackListGuiJson = @"[{""name"": ""HotelBlackList"",""parent"": ""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0.1 0.1 0.1 0.7"",},
                            {""type"":""RectTransform"",""anchormin"": ""{pxmin} {pymin}"",""anchormax"": ""{pxmax} {pymax}""}]},{""parent"": ""HotelBlackList"",""components"":
                            [{""type"":""UnityEngine.UI.Text"",""text"":""{msg}"",""fontSize"":15,""align"": ""MiddleLeft"",},{""type"":""RectTransform"",
                            ""anchormin"": ""0.1 0.1"",""anchormax"": ""1 1""}]}]",
                    MapMarker = "\t\t\t{name} Hotel\r\n{fnum} of {rnum} Rooms Available\r\n{rp} {rc} per {durHours} hours",
                    MapMarkerColor = "#710AC1",
                    MapMarkerColorBorder = "#5FCEA8",
                    MapMarkerRadius = 0.25f,
                    BlackList = new[] { "explosive.timed" },
                    DefaultZoneFlags = new[] { "lootself", "nobuild", "nocup", "nodecay", "noentitypickup", "noplayerloot", "nostash", "notrade", "pvpgod", "sleepgod", "undestr" }
                };
            }
        }


        #region BoilerPlate
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception)
            {
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }

            if (config != null)
            {
                config.AdminGuiJson = config.AdminGuiJson.Replace("{xmin}", config.XMin)
                    .Replace("{xmax}", config.XMax).Replace("{ymin}", config.YMin).Replace("{ymax}", config.YMax);
                config.PlayerGuiJson = config.PlayerGuiJson.Replace("{pxmin}", config.PanelXMin)
                    .Replace("{pxmax}", config.PanelXMax).Replace("{pymin}", config.PanelYMin)
                    .Replace("{pymax}", config.PanelYMax);
                config.BlackListGuiJson = config.BlackListGuiJson.Replace("{pxmin}", config.PanelXMin)
                    .Replace("{pxmax}", config.PanelXMax).Replace("{pymin}", config.PanelYMin)
                    .Replace("{pymax}", config.PanelYMax);

                var blackListTemp = new List<string>();

                if (config.BlackList == null)
                {
                    config.BlackList = blackListTemp.ToArray();
                }

                foreach (var item in config.BlackList)
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

                config.BlackList = blackListTemp.ToArray();
            }

            LoadData();
            LoadPermissions();
        }
        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        
        #endregion
    }
}