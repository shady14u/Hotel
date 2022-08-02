using System.Linq;

namespace Oxide.Plugins
{
    //Define:FileOrder=40
    public partial class Hotel
    {
        private void LoadPermissions()
        {
            permission.RegisterPermission("hotel.admin", this);
            permission.RegisterPermission("hotel.extend", this);
            permission.RegisterPermission("hotel.renter", this);

            foreach (var hotel in _storedData.Hotels.Where(hotel => hotel.p != null && hotel.p.ToLower() != "renter"))
            {
                permission.RegisterPermission("hotel." + hotel.p, this);
            }
        }
    }
}
