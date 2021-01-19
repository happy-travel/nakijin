using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Comparers
{
    public class CountryComparer : IEqualityComparer<Country>
    {
        public bool Equals(Country? first, Country? second)
        {
            if (first == null)
                return second == null;
            if (second == null)
                return false;

            return first.Code == second.Code;
        }

        public int GetHashCode(Country obj) => obj.Code.GetHashCode();
    }
}