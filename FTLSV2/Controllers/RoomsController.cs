using Microsoft.AspNetCore.Mvc;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly FtlsDbContext _context;

        public RoomsController(FtlsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            // 1. Fixed parameters
            string fixedType = "Lecture";
            int fixedMinCapacity = 30;

            // 2. Fixed query
            var query = _context.Rooms
                .Where(r => r.Type.Contains(fixedType) && r.Capacity >= fixedMinCapacity);

            return await query.ToListAsync();
        }
    }
}