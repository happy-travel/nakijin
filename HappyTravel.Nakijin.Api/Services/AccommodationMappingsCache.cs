using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Nakijin.Api.Services
{
    public class AccommodationMappingsCache
    {
        public AccommodationMappingsCache(NakijinContext context)
        {
            _context = context;
        }

        
        public async Task Fill()
        {
            var dbMappings = await _context.HtAccommodationMappings.Where(m => m.IsActive).ToListAsync();
            _accommodationMappings.Clear();
            foreach (var mapping in dbMappings)
            {
                foreach (var mappedHtId in mapping.MappedHtIds)
                    _accommodationMappings.TryAdd(mappedHtId, mapping.HtId);
            }
        }

        
        public async ValueTask<int> GetActualHtId(int mappedHtId)
        {
            if (_accommodationMappings.IsEmpty)
                await Fill();
            
            if (_accommodationMappings.TryGetValue(mappedHtId, out var actualHtId))
                return actualHtId;

            return mappedHtId;
        }


        private readonly ConcurrentDictionary<int, int> _accommodationMappings = new ConcurrentDictionary<int, int>();
        private readonly NakijinContext _context;
    }
}