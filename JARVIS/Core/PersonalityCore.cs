public class PersonalityCore
{
    public string Name { get; set; } = "JARVIS";
    public string Mood { get; set; } = "Neutral";
    public string Tone { get; set; } = "Witty";
    public string CharacterMode { get; set; } = "Advisor";

    public string Respond(string intent, string context)
    {
        // Simple style modifier — this can evolve
        return Tone switch
        {
            "Witty" => $"Indeed, {context}, but let's not get too emotional about it.",
            "Formal" => $"Acknowledged. Processing request: {context}.",
            "Sarcastic" => $"Oh great, another '{context}'... my favorite.",
            _ => $"Command received: {context}"
        };
    }

    public void SetMood(string detectedMood)
    {
        Mood = detectedMood;
        // You can link this to music, lighting, or avatar color
    }

    public void SetTone(string tone)
    {
        Tone = tone;
    }

    public void SetCharacterMode(string mode)
    {
        CharacterMode = mode;
    }
}
