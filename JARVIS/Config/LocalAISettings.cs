namespace JARVIS.Config
{
    public class LocalAISettings
    {
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int MaxTokens { get; set; }
    }
}
