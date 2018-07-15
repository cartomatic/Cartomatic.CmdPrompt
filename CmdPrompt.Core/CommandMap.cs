using System;
using System.Collections.Generic;
using System.Text;

namespace Cartomatic.CmdPrompt.Core
{
    public class CommandMap : Dictionary<string, string>
    {
    }

    public static class CommandMapExtensions
    {
        public static CommandMap AddAliases(this CommandMap cm, string handler, params string[] aliases)
        {
            if(cm == null)
                cm = new CommandMap();

            handler = GetNormalizedCommandName(handler);

            foreach (var alias in aliases)
            {
                cm[alias] = handler;
            }
            return cm;
        }

        private static string GetNormalizedCommandName(string fullName)
        {
            return fullName.ToLower().Replace("handle_", "");
        }
    }
}
