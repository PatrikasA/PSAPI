using System.ComponentModel.DataAnnotations;

namespace Restoranas.Models
{
    public class Visit
    {
        public int apsilankymo_id { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime data { get; set; }

        [Required(ErrorMessage = "Zmoniu skaicius is required")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Zmoniu skaicius must be a valid integer")]
        [Range(1, 20, ErrorMessage = "Zmoniu skaicius must be between 1 and 20")]
        public int zmoniu_skaicius { get; set; }
        public bool apmoketas { get; set; }
        public int? naudotojo_id { get; set; }
        public int staliuko_nr { get; set; }    
        public bool uzbaigtas { get; set; }
    }
}
