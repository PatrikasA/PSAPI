using System.ComponentModel.DataAnnotations;

namespace Restoranas.Models
{
    public class TableInfo
    {
        [Required(ErrorMessage = "Table number is required")]
        [Display(Name = "Table Number")]
        public int TableNumber { get; set; }

        [Required(ErrorMessage = "Seat count is required")]
        [Display(Name = "Seat Count")]
        public int Capacity { get; set; }

    }
}
