using System;

namespace Example
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        delegate (string, int) del();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }

        static (string, int) test()
        {
            return ("", 0);
        }
    }
#endif
}
