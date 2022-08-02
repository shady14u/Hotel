using Oxide.Core;
using System;
using System.Collections.Generic;


namespace Oxide.Plugins
{
    //Define:FileOrder=50
    public partial class Hotel
    {
        private StoredData _storedData;

        public class StoredData
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

        #region BoilerPlate
        private void LoadData()
        {
            try
            {
                _storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Hotel");
            }
            catch (Exception e)
            {
                Puts(e.Message);
                Puts(e.StackTrace);
                _storedData = new StoredData();
            }
        }

        private void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Hotel", _storedData);
        }
        #endregion
    }
}