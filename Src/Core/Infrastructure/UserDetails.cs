namespace TheAssistant.Core.Infrastructure
{
    public class UserDetails
    {
        public string PhoneNumber { get; set; }
        public string PersonalMailTag { get; set; }

        public string WorkMailTag { get; set; }

        public UserDetails(string phoneNumber, string personalMailTag, string workMailTag)
        {
            PhoneNumber = phoneNumber;
            PersonalMailTag = personalMailTag;
            WorkMailTag = workMailTag;
        }
    }
}
