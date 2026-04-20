using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace FTLSV2.Models
{
    [Table("faculty_load_summary")]

    [Keyless]
    public class FacultyLoadSummary
    {
        // This entity maps to a view/table that exposes first/last name and a total load.
        // The underlying source doesn't provide a stable primary key column, so this
        // entity will be configured as keyless in the DbContext.

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        // Some databases return loads as decimals (e.g. 1.00). Keep decimal here and
        // expose an integer-friendly CurrentLoad for existing UI code.
        [Column("total_load")]
        public decimal TotalLoad { get; set; }

        // email is now provided by the view
        [Column("email")]
        public string Email { get; set; }

        // Computed name used by the Razor page (keeps previous code working)
        [NotMapped]
        public string Name => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        // Provide an integer load for existing UI (rounds down/up as appropriate)
        [NotMapped]
        public int CurrentLoad => (int)System.Math.Round(TotalLoad);
    }
}
