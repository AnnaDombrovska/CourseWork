using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Models
{
    public class FavoriteHotel
    {
        [Key]
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string HotelId { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
        public string Rating { get; set; }
        public string Url { get; set; }
    }
}
