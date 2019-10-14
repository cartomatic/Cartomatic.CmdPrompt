using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartomatic.CmdPrompt.Core
{
    public static class ConsoleEx
    {
        private static ConsoleColor SetConsoleColor(ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            return currentColor;
        }

        private static void ResetConsoleColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        public static void Write(string str, ConsoleColor color)
        {
            var colorBefore = SetConsoleColor(color);
            Console.Write(str);
            ResetConsoleColor(colorBefore);
        }

        public static void WriteLine(string str, ConsoleColor color)
        {
            var colorBefore = SetConsoleColor(color);
            Console.WriteLine(str);
            ResetConsoleColor(colorBefore);
        }

        public static void WriteErr(string str)
        {
            WriteLine(str, ConsoleColor.DarkRed);
        }
        public static void WriteOk(string str = "Ok!")
        {
            WriteLine(str, ConsoleColor.DarkGreen);
        }
    }
}
