using System.ComponentModel;

namespace HappyTravel.Nakijin.Data.Models
{
    public enum Suppliers
    {
        Unknown = 0,
        Netstorming = 1,
        [Description("iWTX")]
        Illusions = 2,
        [Description("Direct Contracts")]
        DirectContracts = 3,
        [Description("RateHawk")]
        Etg = 4,
        [Description("Rakuten Travel Xchange")]
        Rakuten = 5,
        [Description("Columbus (Direct Contracts)")]
        Columbus = 6,
        [Description("TravelgateX (TEST)")]
        TravelgateXTest = 7,
        [Description("Jumeirah Hotels")]
        Jumeirah = 8
    }
}