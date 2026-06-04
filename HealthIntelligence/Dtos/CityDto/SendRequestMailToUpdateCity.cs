using HealthIntelligence.Models;

namespace HealthIntelligence.Dtos.CityDto
{
    public class SendRequestMailToUpdateCity
    {
        public int UserID { get; set; }
        public int MailToUserID { get; set; }
        public int UserCityMappingID { get; set; }
    }
}
