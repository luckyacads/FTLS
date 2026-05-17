using Microsoft.AspNetCore.Mvc;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly FtlsDbContext _context;

        public DepartmentsController(FtlsDbContext context)
        {
            _context = context;
        }

        // Notice the parentheses () are now EMPTY! No user input allowed.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
        {
            // 1. Define your FIXED parameters right here in the code
            int fixedSchoolId = 6;

            // 2. Tell the database to ALWAYS use that fixed rule
            var query = _context.Departments.Where(d => d.SchoolId == fixedSchoolId);

            // 3. Return the results
            return await query.ToListAsync();
        }
    }
}