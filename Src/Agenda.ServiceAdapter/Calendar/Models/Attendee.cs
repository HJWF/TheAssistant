namespace TheAssistant.Agenda.ServiceAdapter.Calendar.Models
{
    public class Attendee
    {
        public string type { get; set; } = string.Empty;
        public Status status { get; set; } = new Status();
        public Emailaddress emailAddress { get; set; } = new Emailaddress();
    }


}
