using Microsoft.AspNetCore.Mvc;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchoolsController : ControllerBase
    {
        private readonly FtlsDbContext _context;

        public SchoolsController(FtlsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<School>>> GetSchools()
        {
            // 1. Fixed parameter
            string fixedCode = "SCS";

            // 2. Fixed query
            var query = _context.Schools
                .Where(s => s.Code == fixedCode);

            return await query.ToListAsync();
        }
    }
}