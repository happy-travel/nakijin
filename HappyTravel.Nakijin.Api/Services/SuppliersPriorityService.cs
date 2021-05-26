using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Nakijin.Api.Services
{
    public class SuppliersPriorityService : ISuppliersPriorityService
    {
        public SuppliersPriorityService(NakijinContext context)
        {
            _context = context;
        }


        public async Task AddOrUpdate(Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority)
        {
            var jsonDocument = JsonDocument.Parse(JsonConvert.SerializeObject(suppliersPriority));
            var dbPriorities = await _context.StaticDatas.SingleOrDefaultAsync(d => d.Type == StaticDataTypes.SuppliersPriority);
            if (dbPriorities != default)
            {
                dbPriorities.Value = jsonDocument;
                _context.Update(dbPriorities);
            }
            else
            {
                _context.StaticDatas.Add(new StaticData
                {
                    Type = StaticDataTypes.SuppliersPriority,
                    Value = jsonDocument
                });
            }

            await _context.SaveChangesAsync();
        }


        public async ValueTask<Dictionary<AccommodationDataTypes, List<Suppliers>>> Get()
        {
            if (_suppliersPriority.Any())
                return _suppliersPriority;

            var priorityDocument =
                (await _context.StaticDatas.SingleAsync(d => d.Type == StaticDataTypes.SuppliersPriority)).Value;
            _suppliersPriority =
                JsonConvert.DeserializeObject<Dictionary<AccommodationDataTypes, List<Suppliers>>>(
                    priorityDocument.RootElement.ToString()!);
            return _suppliersPriority;
        }


        private Dictionary<AccommodationDataTypes, List<Suppliers>> _suppliersPriority = new Dictionary<AccommodationDataTypes, List<Suppliers>>();

        private readonly NakijinContext _context;
    }
}