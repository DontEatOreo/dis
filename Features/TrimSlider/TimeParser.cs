namespace dis.Features.TrimSlider;

public static class TimeParser
{
    public static bool IsValidTimeChar(char c) => 
        char.IsDigit(c) || c is ':' or '.';

    public static bool TryParseTimeInput(string input, out double seconds)
    {
        seconds = 0;

        if (double.TryParse(input, out seconds))
            return true;

        var parts = input.Split(':');
        if (parts.Length != 2 || 
            !int.TryParse(parts[0], out var minutes) || 
            !double.TryParse(parts[1], out var secs)) 
            return false;
                
        seconds = minutes * 60 + secs;
        return true;
    }
}
