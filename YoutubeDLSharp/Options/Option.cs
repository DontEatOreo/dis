﻿using System.ComponentModel;
using System.Globalization;

namespace YoutubeDLSharp.Options;

/// <summary>
/// Represents one youtube-dl option.
/// </summary>
/// <typeparam name="T">The type of the option.</typeparam>
public class Option<T> : IOption
{
    private T _value;

    /// <summary>
    /// The default string representation of the option flag.
    /// </summary>
    public string DefaultOptionString => OptionStrings.Last();

    /// <summary>
    /// An array of all possible string representations of the option flag.
    /// </summary>
    public string[] OptionStrings { get; }

    /// <summary>
    /// True if the option flag is set; false otherwise.
    /// </summary>
    public bool IsSet { get; private set; }

    /// <summary>
    /// The option value.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            IsSet = !Equals(value, default(T));
            _value = value;
        }
    }
        
    /// <summary>
    /// True if this option is custom.
    /// </summary>
    public bool IsCustom { get; }

    /// <summary>
    /// Creates a new instance of class Option.
    /// </summary>
    public Option(params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
    }

    public Option(bool isCustom, params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
        IsCustom = isCustom;
    }

    /// <summary>
    /// Sets the option value from a given string representation.
    /// </summary>
    /// <param name="s">The string (including the option flag).</param>
    public void SetFromString(string s)
    {
        string[] split = s.Split(' ');
        string stringValue = s.Substring(split[0].Length).Trim().Trim('"');
        if (!OptionStrings.Contains(split[0]))
            throw new ArgumentException("Given string does not match required format.");
        if (Value is bool)
        {
            Value = (T)(object)OptionStrings.Contains(s);
        }
        else if (Value is Enum)
        {
            string titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stringValue);
            Value = (T)Enum.Parse(typeof(T), titleCase);
        }
        else if (Value is DateTime)
        {
            Value = (T)(object)DateTime.ParseExact(stringValue, "yyyyMMdd", null);
        }
        else
        {
            var conv = TypeDescriptor.GetConverter(typeof(T));
            Value = (T)conv.ConvertFrom(stringValue);
        }
    }

    public override string ToString()
    {
        if (!IsSet) return String.Empty;
        var val = Value switch
        {
            bool => string.Empty,
            Enum => $" \"{Value.ToString()?.ToLower()}\"",
            DateTime dateTime => $" {dateTime:yyyyMMdd}",
            string => $" \"{Value}\"",
            _ => " " + Value
        };
        return DefaultOptionString + val;
    }
}