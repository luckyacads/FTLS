using Microsoft.AspNetCore.Mvc;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly FtlsDbContext _context;

        public UsersController(FtlsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            // 1. Fixed parameters
            string fixedRole = "Teacher";
            string fixedStatus = "Active";

            // 2. Fixed query
            var query = _context.Users
                .Where(u => u.Role == fixedRole && u.Status == fixedStatus);

            return await query.ToListAsync();
        }
    }
}