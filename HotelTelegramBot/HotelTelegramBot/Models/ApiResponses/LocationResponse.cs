using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Models.ApiResponses
{
    public class LocationResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<LocationData> Data { get; set; }
    }

    public class LocationData
    {
        public string Title { get; set; }
        public int GeoId { get; set; }
        public string SecondaryText { get; set; }
    }
}
