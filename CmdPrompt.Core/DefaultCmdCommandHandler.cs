using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cartomatic.CmdPrompt.Core
{
    /// <summary>
    /// Default command handler
    /// </summary>
    public class DefaultCmdCommandHandler : ICmdCommandHandler
    {
        /// <summary>
        /// whether or not the handler's exit command has been called
        /// </summary>
        bool _exit = false;

        /// <summary>
        /// Alias map used to manage command aliases
        /// </summary>
        Dictionary<string, string> _aliasMap;

        private readonly string _cmdInfo = "Default cmd handler... v 1.0.0";

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="cmdInfo"></param>
        public DefaultCmdCommandHandler(string cmdInfo)
        {
            if (!string.IsNullOrEmpty(cmdInfo))
            {
                _cmdInfo = cmdInfo;
            }
            SetUpDefaultAliasMap();
        }

        /// <summary>
        /// ccreates an instance
        /// </summary>
        public DefaultCmdCommandHandler()
        {
            SetUpDefaultAliasMap();
        }

        /// <summary>
        /// Sets up the command map
        /// </summary>
        private void SetUpDefaultAliasMap()
        {
            _aliasMap = new Dictionary<string, string>()
            {
                {"e","exit"}, {"quit","exit"}, {"q","exit"}, { "fuckoff", "exit" }, { "spierdalaj", "exit" }
            };
        }

        /// <summary>
        /// A hook to set up extra commands or replace the default mapping
        /// </summary>
        /// <param name="aliases"></param>
        /// <param name="overwrite"></param>
        public void SetUpCommandMap(Dictionary<string, string> aliases, bool overwrite = false)
        {
            if (overwrite)
            {
                _aliasMap = aliases ?? new Dictionary<string, string>();
            }
            else
            {
                foreach (var key in aliases.Keys)
                {
                    _aliasMap[key] = aliases[key];
                }
            }
        }

        /// <summary>
        /// Prints handler specific info; used to give some init info like cmd version and stuff
        /// </summary>
        public virtual void PrintHandlerInfo()
        {
            ConsoleEx.WriteLine(_cmdInfo, ConsoleColor.DarkRed);
            ConsoleEx.WriteLine("Hi there!", ConsoleColor.DarkRed);
            Console.WriteLine();
        }

        /// <summary>
        /// Returns the state of the exit flag; client should exit if true
        /// </summary>
        /// <returns></returns>
        public virtual bool Exit()
        {
            return _exit;
        }

        /// <summary>
        /// Handles a cmd command
        /// </summary>
        /// <param name="command"></param>
        public virtual async Task HandleCommand(string command)
        {
            IDictionary<string,string> args = null;
            var nc = NormaliseCommand(command, out args);

            var t = this.GetType();
            var m = t.GetMethod(
                "Handle_" + nc,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase
            );

            if (m != null)
            {
                var pi = m.GetParameters();
                object[] p = null;

                if (pi.Length > 0)
                {
                    //Note: just pass in commands
                    p = new object[] { args };
                }

                var async = m.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
                if (async)
                {
                    await (Task)m.Invoke(this, p);
                }
                else
                {
                    m.Invoke(this, p);
                }
            }
            else
            {
                //just in case the mapping is defined, but method is not
                HandleUnknown(command);
            }
        }
        
        /// <summary>
        /// Looks up the command map and returns a normalised command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected virtual string NormaliseCommand(string command, out IDictionary<string, string> arguments)
        {
            var outCommand = "not_recognised";
            arguments = null;

            var args = command.Split(' ');

            if (!GetSupportedCommandNames(discardPrivate: false).Contains(args[0]))
            {
                try
                {
                    //command handler unknown, so need to check the alias map
                    outCommand = _aliasMap[args[0].ToLower()];
                }
                catch
                {
                    //ignore
                }
            }
            else
            {
                outCommand = args[0];
            }
            

            //extract args if it makes sense
            if (outCommand != "not_recognised")
            {
                arguments = new Dictionary<string, string>();

                //extract all params but command!
                for (var a = 1; a < args.Length; a++)
                {
                    var paramParts = args[a].Split(':');

                    if (paramParts.Length == 0)
                        continue;

                    var pName = paramParts[0];
                    arguments[pName] = paramParts.Length == 2 ? paramParts[1] : string.Empty;
                }
            }

            return outCommand;
        }

        /// <summary>
        /// Handles exit command
        /// </summary>
        protected virtual void Handle_Exit()
        {
            Console.WriteLine("Bye, bye...");
            Console.WriteLine();
            _exit = true;
        }

        /// <summary>
        /// Hnadles cls command
        /// </summary>
        protected virtual void Handle_Cls()
        {
            Console.Clear();
        }

        /// <summary>
        /// Justg a test command
        /// </summary>
        /// <param name="args"></param>
        /// <param name="help"></param>
        protected virtual void Handle_SelfTest(IDictionary<string, string> args)
        {
            if (GetHelp(args))
            {
                Console.WriteLine("This is a selftest help. The command is registered properly!");
                Console.WriteLine();

                return;
            }

            Console.WriteLine("This is a selftest command output. The command is registered properly!");
            Console.WriteLine();
        }

        /// <summary>
        /// Handles generic help
        /// </summary>
        protected virtual void Handle_Help()
        {
            ConsoleEx.WriteLine($"{_cmdInfo} :: help...", ConsoleColor.DarkGreen);
            Console.WriteLine();

            PrintCommands();

            ConsoleEx.WriteLine("Type 'command help' to get a detailed help on a particular command", ConsoleColor.DarkGreen);
            Console.WriteLine();
        }


        //Utils
        //----------------------------------------------------------------------------------------------------------------------------

        protected virtual void HandleUnknown(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                ConsoleEx.WriteLine($"Sorry mate, but '{command}' is not something I recognise...", ConsoleColor.DarkRed);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Whether or not help was requested in args
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool GetHelp(IDictionary<string, string> args)
        {
            return args.Keys.Any(k => k.ToLower() == "help");
        }

        /// <summary>
        /// Returns a name of a calling method
        /// </summary>
        /// <param name="callerNameName"></param>
        /// <returns></returns>
        protected virtual string GetCallerName([System.Runtime.CompilerServices.CallerMemberName] string callerNameName = "")
        {
            return callerNameName.ToLower().Replace("handle_", "");
        }

        /// <summary>
        /// extracts command names supported by this command handler
        /// </summary>
        /// <param name="discardPrivate"></param>
        /// <returns></returns>
        protected IEnumerable<string> GetSupportedCommandNames(bool discardPrivate = true)
        {
            var t = this.GetType();
            var methods = t.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                             .Where(m => m.Name.ToLower().StartsWith("handle_"))
                             .Select(m => m.Name.ToLower().Replace("handle_", ""));
            return discardPrivate
                ? methods.Where(m => m != "selftest")
                : methods;
        } 


        /// <summary>
        /// Prints currently supported commands
        /// </summary>
        protected virtual void PrintCommands()
        {
            Console.WriteLine("Supported commands are:");

            var methods = GetSupportedCommandNames();

            foreach (var method in GetSupportedCommandNames())
            {
                var aliases = _aliasMap.Where(cm => cm.Value == method && cm.Key != method).Select(cm => cm.Key).ToList();

                ConsoleEx.Write(method, ConsoleColor.DarkMagenta);

                ConsoleEx.Write(aliases.Count > 0 ? "; aliases: " + string.Join(", ", aliases) : "", ConsoleColor.DarkBlue);

                Console.Write(Environment.NewLine);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Extracts string param value off the args
        /// </summary>
        /// <param name="pName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual string ExtractParam(string pName, Dictionary<string, string> args)
        {
            return ExtractParam<string>(pName, args);
        }

        /// <summary>
        /// Extracts param vakue off the args and converts is to the appropriate type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual T ExtractParam<T>(string pName, IDictionary<string, string> args)
        {
            object pValue = default(T);

            if (args.ContainsKey(pName))
            {
                var stringPValue = args[pName];

                if (typeof (T) == typeof (int) || typeof(T) == typeof(int?))
                {
                    int intValue;
                    if (int.TryParse(stringPValue, out intValue))
                        pValue = intValue;
                }
                if (typeof (T) == typeof (bool) || typeof (T) == typeof (bool?))
                {
                    bool boolValue;
                    if (bool.TryParse(NormaliseBoolStr(stringPValue), out boolValue))
                        pValue = boolValue;
                }
                //else if ()
                else
                {
                    pValue = stringPValue;
                }
            }

            return (T)pValue;
        }

        /// <summary>
        /// Normalises a bool input string so strings like t, 1, f, 0 can also be recognised
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        protected string NormaliseBoolStr(string v)
        {
             var outV = v;

            switch (v.ToLower())
            {
                case "1":
                case "t":
                    outV = "true";
                    break;

                case "f":
                case "0":
                    outV = "false";
                    break;
            }

            return outV;
        }

        /// <summary>
        /// Prompts user to answer a specified question
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public bool PromptUser(string question)
        {
            ConsoleEx.WriteLine($"{question}", ConsoleColor.DarkYellow);
            ConsoleEx.WriteLine("Y/N", ConsoleColor.DarkYellow);

            var ok = false;
            var done = false;

            while (!done) //infinite loop
            {
                var line = (Console.ReadLine() ?? "").ToLower();

                if (line == "n")
                {
                    done = true;
                }
                else if (line == "y")
                {
                    ok = true;
                    done = true;
                }
                else
                {
                    ConsoleEx.WriteLine("Only Y or N please...", ConsoleColor.DarkRed);
                }
            }

            return ok;
        }

    }
}
