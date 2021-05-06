using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.Mappers.Enums;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Services.Workers
{
    public interface IAccommodationMapper
    {
        Task MapAccommodations(List<Suppliers> suppliers, MappingTypes mappingType, CancellationToken token);
    }
}