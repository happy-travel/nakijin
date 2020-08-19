namespace HappyTravel.SecurityClient
{
    public class TokenRequestOptions
    {
        public string Address { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GrantType { get; set; }
        public string Scope { get; set; }
    }
}
