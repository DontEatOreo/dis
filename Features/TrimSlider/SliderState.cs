namespace dis.Features.TrimSlider;

public sealed class SliderState(TimeSpan duration)
{
    public double StartPosition { get; private set; }
    public double EndPosition { get; private set; } = duration.TotalSeconds;
    public bool IsAdjustingStart { get; private set; } = true;
    public string NumberBuffer { get; private set; } = "";
    public bool IsTypingNumber { get; private set; }
    public bool IsCancelled { get; private set; }

    public void RoundPositions()
    {
        StartPosition = Math.Round(StartPosition, 3);
        EndPosition = Math.Round(EndPosition, 3);
    }

    public bool SelectStart()
    {
        IsAdjustingStart = true;
        return false;
    }

    public bool SelectEnd()
    {
        IsAdjustingStart = false;
        return false;
    }

    public bool StartTyping()
    {
        IsTypingNumber = true;
        NumberBuffer = "";
        return false;
    }

    public void ResetNumberInput()
    {
        NumberBuffer = "";
        IsTypingNumber = false;
    }

    public void HandleBackspace()
    {
        if (NumberBuffer.Length > 0)
            NumberBuffer = NumberBuffer[..^1];
    }

    public void AppendToBuffer(char c)
    {
        if (TimeParser.IsValidTimeChar(c))
            NumberBuffer += c;
    }

    public (double min, double max) GetValidRange() =>
        IsAdjustingStart
            ? (0, EndPosition - Constants.MillisecondStep)
            : (StartPosition, duration.TotalSeconds);

    public void UpdatePosition(double seconds)
    {
        if (IsAdjustingStart)
            StartPosition = seconds;
        else
            EndPosition = seconds;
    }

    public bool AdjustValue(double step, TimeSpan timeSpan)
    {
        if (IsAdjustingStart)
        {
            var newStart = StartPosition + step;
            StartPosition = Math.Max(0, Math.Min(EndPosition - Constants.MillisecondStep, newStart));
        }
        else
        {
            var newEnd = EndPosition + step;
            EndPosition = Math.Max(StartPosition + Constants.MillisecondStep, Math.Min(timeSpan.TotalSeconds, newEnd));
        }
        return false;
    }

    public string FormatRange() =>
        $@"{TimeSpan.FromSeconds(StartPosition):mm\:ss\.fff} - {TimeSpan.FromSeconds(EndPosition):mm\:ss\.fff}";

    public IEnumerable<string> GenerateSliderCharacters(int width)
    {
        var startIndex = (int)(StartPosition / duration.TotalSeconds * width);
        var endIndex = (int)(EndPosition / duration.TotalSeconds * width);

        // Ensure endIndex is visible when at maximum
        if (EndPosition >= duration.TotalSeconds && endIndex == width)
            endIndex = width - 1;

        for (var i = 0; i < width; i++)
        {
            if (i == startIndex)
                yield return IsAdjustingStart ? "[blue]┃[/]" : "┃";
            else if (i == endIndex)
                yield return IsAdjustingStart ? "┃" : "[blue]┃[/]";
            else if (i >= startIndex && i <= endIndex)
                yield return "[green]━[/]";
            else
                yield return "[grey]─[/]";
        }
    }

    public bool CancelOperation()
    {
        IsCancelled = true;
        return true;
    }
}
