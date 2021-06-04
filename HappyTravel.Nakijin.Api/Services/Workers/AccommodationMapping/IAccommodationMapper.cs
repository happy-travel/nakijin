using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.Mappers.Enums;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationMapping
{
    public interface IAccommodationMapper
    {
        Task MapAccommodations(List<Suppliers> suppliers, MappingTypes mappingType, CancellationToken token);
    }
}