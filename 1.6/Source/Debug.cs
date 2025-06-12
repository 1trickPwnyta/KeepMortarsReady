namespace KeepMortarsReady
{
    public static class Debug
    {
        public static void Log(string message)
        {
#if DEBUG
            Verse.Log.Message("[Keep Mortars Ready] " + message);
#endif
        }
    }
}
