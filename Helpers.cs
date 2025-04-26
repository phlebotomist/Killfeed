namespace PvPDetails;
public static class Helpers
{
    public static string Pad(string s, int n)
    {
        return new string(' ', n) + s + new string(' ', n);
    }

    public static string PadR(string s, int n)
    {
        return s + new string(' ', n);
    }
    public static string PadL(string s, int n)
    {
        return new string(' ', n) + s;
    }
}