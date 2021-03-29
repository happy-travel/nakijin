using System.Collections.Generic;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Comparers
{
    public class LocalityComparer :IEqualityComparer<Locality>
    {
        public bool Equals(Locality? first, Locality? second)
        {
            if (first == null)
                return second == null;
            if (second == null)
                return false;

            return first.CountryId == second.CountryId
                && first.Names.En == second.Names.En;
        }

        public int GetHashCode(Locality obj) => (obj.CountryId, obj.Names.En).GetHashCode();
    }
}