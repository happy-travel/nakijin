using System.Collections.Generic;

namespace HappyTravel.PropertyManagement.Data.Models.Accommodations
{
    public class ContactInfo
    {
        public List<string> Emails { get; set; } = new List<string>();

        public List<string> Faxes { get; set; } = new List<string>();

        public List<string> Phones { get; set; } = new List<string>();

        public List<string> WebSites { get; set; } = new List<string>();
    }
}