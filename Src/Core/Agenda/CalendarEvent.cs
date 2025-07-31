namespace TheAssistant.Core.Agenda
{
    public class CalendarEvent
    {
        public string Subject { get; set; } 
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Location { get; set; }
        public string Organizer { get; set; }

        public CalendarEvent(string subject, DateTime start, DateTime end, string location, string organizer)
        {
            Subject = subject;
            Start = start;
            End = end;
            Location = location;
            Organizer = organizer;
        }
    }
}