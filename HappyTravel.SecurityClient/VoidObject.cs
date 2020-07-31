namespace HappyTravel.SecurityTokenManager
{
    public sealed class VoidObject
    {
        private VoidObject()
        { }


        public static readonly VoidObject Instance = new VoidObject();
    }
}
