namespace HealthIntelligence.Dtos.PublicDto
{
    public class PartnerCountryFilterResponse
    {
        public List<string> Cities { get; set; }
        public List<string> Regions { get; set; }
        public List<PartnerCountryDto> Countries { get; set; }
    }

    public class PartnerCountryDto
    {
        public int CountryID { get; set; }
        public string CountryName { get; set; }
    }
}
