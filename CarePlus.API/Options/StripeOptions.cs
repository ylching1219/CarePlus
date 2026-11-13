namespace CarePlus.API.Options
{
    public class StripeOptions
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public string Currency { get; set; } = "myr";
    }
}
