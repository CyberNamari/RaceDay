namespace RaceDay.Web.Services;

public static class Format
{
    public static string Ordinal(int n)
    {
        var suffix = (n % 100) is 11 or 12 or 13
            ? "th"
            : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n + suffix;
    }

    // e.g. "47th out of 312 finishers"
    public static string Position(int? position, int? total)
    {
        if (position is null) return "-";
        if (total is null or 0) return Ordinal(position.Value);
        return $"{Ordinal(position.Value)} out of {total} finishers";
    }
}