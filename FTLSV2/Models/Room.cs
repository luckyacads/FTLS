using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    public class Room
    {
        // The new DB schema uses `room_id` as the primary key (integer)
        [Key]
        [Column("room_id")]
        public int RoomId { get; set; }

        // Human readable room name stored in `room_name`
        [Column("room_name")]
        public string Name { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("capacity")]
        public int Capacity { get; set; }

    }
}