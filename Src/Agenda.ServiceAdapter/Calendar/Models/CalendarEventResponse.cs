namespace TheAssistant.Agenda.ServiceAdapter.Calendar.Models
{
    public class CalendarEventResponse
    {
        public string odataetag { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public DateTime createdDateTime { get; set; } = new DateTime();
        public DateTime lastModifiedDateTime { get; set; } = new DateTime();
        public string changeKey { get; set; } = string.Empty;
        public object[] categories { get; set; } = Array.Empty<object>();
        public string transactionId { get; set; } = string.Empty;
        public string originalStartTimeZone { get; set; } = string.Empty;
        public string originalEndTimeZone { get; set; } = string.Empty;
        public string iCalUId { get; set; } = string.Empty;
        public int reminderMinutesBeforeStart { get; set; }
        public bool isReminderOn { get; set; }
        public bool hasAttachments { get; set; }
        public string subject { get; set; } = string.Empty;
        public string bodyPreview { get; set; } = string.Empty;
        public string importance { get; set; } = string.Empty;
        public string sensitivity { get; set; } = string.Empty;
        public bool isAllDay { get; set; }
        public bool isCancelled { get; set; }
        public bool isOrganizer { get; set; }
        public bool responseRequested { get; set; }
        public object seriesMasterId { get; set; } = string.Empty;
        public string showAs { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string webLink { get; set; } = string.Empty;
        public object onlineMeetingUrl { get; set; } = string.Empty;
        public bool isOnlineMeeting { get; set; }
        public string onlineMeetingProvider { get; set; } = string.Empty;
        public bool allowNewTimeProposals { get; set; }
        public bool isDraft { get; set; }
        public bool hideAttendees { get; set; }
        public object recurrence { get; set; } = string.Empty;
        public Responsestatus responseStatus { get; set; } = new Responsestatus();
        public Body body { get; set; } = new Body();
        public Start start { get; set; } = new Start();
        public End end { get; set; } = new End();
        public Location location { get; set; } = new Location();
        public Location[] locations { get; set; } = Array.Empty<Location>();
        public Attendee[] attendees { get; set; } = Array.Empty<Attendee>();
        public Organizer organizer { get; set; } = new Organizer();
        public Onlinemeeting onlineMeeting { get; set; } = new Onlinemeeting();
    }
}
