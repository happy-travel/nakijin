using System.Collections.Generic;
using HappyTravel.Nakijin.Data.Models;
using Microsoft.OpenApi.Any;

namespace HappyTravel.Nakijin.Api.Comparers
{
    public class LocalityZoneComparer : IEqualityComparer<LocalityZone>
    {
        public bool Equals(LocalityZone? first, LocalityZone? second)
        {
            if (first == null)
                return second == null;
            if (second == null)
                return false;

            return first.LocalityId == second.LocalityId
                && first.Names.En == second.Names.En;
        }

        public int GetHashCode(LocalityZone obj) => (obj.LocalityId + obj.Names.En).GetHashCode();
    }
}