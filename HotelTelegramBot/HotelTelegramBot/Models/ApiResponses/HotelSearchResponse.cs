using System.Collections.Generic;

namespace HotelTelegramBot.Models.ApiResponses
{
    public class HotelSearchResponse
    {
        public bool Status { get; set; }
        public InnerDataWrapper Data { get; set; }
    }

    public class InnerDataWrapper
    {
        public List<HotelData> Data { get; set; }
    }

    public class HotelData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string PriceForDisplay { get; set; }
        public BubbleRating BubbleRating { get; set; }

        public List<CardPhoto> CardPhotos { get; set; }
    }

    public class BubbleRating
    {
        public double? Rating { get; set; }
    }

    public class CardPhoto
    {
        public PhotoSizes Sizes { get; set; }
    }

    public class PhotoSizes
    {
        public string UrlTemplate { get; set; }
    }
}