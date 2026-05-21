using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Models
{
    public class Hotel
    {
        public string Id { get; set; } 
        public string Name { get; set; }
        public string Price { get; set; }
        public string Rating { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }
}
