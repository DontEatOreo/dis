using System.Globalization;
using Spectre.Console;

namespace dis.Features.TrimSlider;

public sealed class TrimmingSlider(TimeSpan duration)
{
    private readonly TimeSpan _duration = duration > TimeSpan.Zero 
        ? duration 
        : throw new ArgumentException("Duration must be positive", nameof(duration));

    private readonly SliderState _state = new(duration);

    public string ShowSlider()
    {
        while (true)
        {
            RenderInterface();

            var key = Console.ReadKey(true);
            
            if (_state.IsTypingNumber)
            {
                HandleNumberInput(key);
                continue;
            }

            if (HandleNavigationKey(key))
            {
                return FormatResult();
            }

            _state.RoundPositions();
        }
    }

    private void RenderInterface()
    {
        AnsiConsole.Clear();
        DrawSlider();
        DrawInstructions();
    }

    private void DrawInstructions()
    {
        if (_state.IsTypingNumber)
        {
            AnsiConsole.Write(DisplayStrings.GetTimeInput(_state.NumberBuffer));
        }
        else
        {
            AnsiConsole.Write(DisplayStrings.Controls);
        }
    }

    private void HandleNumberInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter when !string.IsNullOrEmpty(_state.NumberBuffer):
                ProcessTimeInput();
                break;
            case ConsoleKey.Escape:
                _state.ResetNumberInput();
                break;
            case ConsoleKey.Backspace:
                _state.HandleBackspace();
                break;
            default:
                _state.AppendToBuffer(key.KeyChar);
                break;
        }
    }

    private void ProcessTimeInput()
    {
        if (!TimeParser.TryParseTimeInput(_state.NumberBuffer, out var seconds)) 
            return;

        var (min, max) = _state.GetValidRange();
        if (seconds >= min && seconds <= max)
        {
            _state.UpdatePosition(seconds);
        }
        _state.ResetNumberInput();
    }

    private bool HandleNavigationKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            _state.CancelOperation();
            return true;
        }

        var step = (key.Modifiers & ConsoleModifiers.Shift) != 0 ? Constants.MillisecondStep : Constants.SecondStep;
        
        return key.Key switch
        {
            ConsoleKey.D1 => SetResult(_state.SelectStart()),
            ConsoleKey.D2 => SetResult(_state.SelectEnd()),
            ConsoleKey.Spacebar => SetResult(_state.StartTyping()),
            ConsoleKey.Enter => true,
            ConsoleKey.UpArrow => SetResult(_state.AdjustValue(Constants.MinuteStep, _duration)),
            ConsoleKey.DownArrow => SetResult(_state.AdjustValue(-Constants.MinuteStep, _duration)),
            ConsoleKey.LeftArrow => SetResult(_state.AdjustValue(-step, _duration)),
            ConsoleKey.RightArrow => SetResult(_state.AdjustValue(step, _duration)),
            _ => false
        };
    }

    private static bool SetResult(bool result) => result;

    private void DrawSlider()
    {
        var slider = CreateSliderVisualization();
        
        AnsiConsole.MarkupLine($"\nVideo duration: [blue]{FormatTime(_duration)}[/]");
        AnsiConsole.MarkupLine(
            $"Selected range: [green]{_state.FormatRange()}[/]\n");
        AnsiConsole.MarkupLine($"Currently adjusting: [blue]{(_state.IsAdjustingStart ? "Start" : "End")}[/] position\n");
        AnsiConsole.MarkupLine($"0s {slider} {_duration.TotalSeconds:F2}s");
    }

    private string CreateSliderVisualization() =>
        string.Join("", _state.GenerateSliderCharacters(Constants.SliderWidth));

    private static string FormatTime(TimeSpan time) => 
        $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";

    private string FormatResult()
    {
        return IsCancelled() 
            ? string.Empty 
            : $"{_state.StartPosition.ToString(CultureInfo.InvariantCulture)}-{_state.EndPosition.ToString(CultureInfo.InvariantCulture)}";
    }

    private bool IsCancelled() => _state.IsCancelled;
}
