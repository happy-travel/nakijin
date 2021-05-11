using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.Mappers.Enums;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationsMapping
{
    public interface IAccommodationsMapper
    {
        Task MapAccommodations(List<Suppliers> suppliers, MappingTypes mappingType, CancellationToken token);
    }
}