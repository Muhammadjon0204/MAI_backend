namespace MAI.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
        public DateTime? SubscriptionExpiry { get; set; }
        public int DailyQueriesUsed { get; set; } = 0;
        public DateTime LastQueryReset { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum SubscriptionTier
    {
        Free = 0,
        Pro = 1,
        Premium = 2
    }
}