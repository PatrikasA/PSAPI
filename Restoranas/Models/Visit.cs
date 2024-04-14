namespace Restoranas.Models
{
    public class Visit
    {
        public int apsilankymo_id { get; set; }
        public DateTime data { get; set; }
        public int zmoniu_skaicius { get; set; }
        public bool apmoketas { get; set; }
        public int naudotojo_id { get; set; }
        public int staliuko_nr { get; set; }
        public bool uzbaigtas { get; set; }
    }
}
