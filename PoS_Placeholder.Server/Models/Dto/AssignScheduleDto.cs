using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto
{
    public class AssignScheduleDto
    {
        [Required]
        public DateTime Day { get; set; }
        [Required]
        public TimeOnly StartTime { get; set; }
        [Required]
        public TimeOnly EndTime { get; set; }
        public TimeOnly? BreakStart { get; set; }
        public TimeOnly? BreakEnd { get; set; }
    }

}
