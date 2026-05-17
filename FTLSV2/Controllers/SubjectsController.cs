using Microsoft.AspNetCore.Mvc;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectsController : ControllerBase
    {
        private readonly FtlsDbContext _context;

        public SubjectsController(FtlsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subject>>> GetSubjects()
        {
            // 1. Fixed parameter
            int fixedUnits = 3;

            // 2. Fixed query
            var query = _context.Subjects
                .Where(s => s.Units == fixedUnits);

            return await query.ToListAsync();
        }
    }
}