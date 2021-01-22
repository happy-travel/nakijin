using System;

namespace HappyTravel.StaticDataMapper.Api.Filters.Authorization
{
    [Flags]
    public enum MapperPermissions
    {
        Read = 1,
        Edit = 2
    }
}