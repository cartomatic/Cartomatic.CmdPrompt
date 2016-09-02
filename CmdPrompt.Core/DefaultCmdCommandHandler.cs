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
        /// Command map used to manage command aliases
        /// </summary>
        Dictionary<string, string> _commandMap;

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
            SetUpDefaultCommandMap();
        }

        /// <summary>
        /// ccreates an instance
        /// </summary>
        public DefaultCmdCommandHandler()
        {
            SetUpDefaultCommandMap();
        }

        /// <summary>
        /// Sets up the command map
        /// </summary>
        private void SetUpDefaultCommandMap()
        {
            _commandMap = new Dictionary<string, string>()
            {
                {"exit","exit"}, {"e","exit"}, {"quit","exit"}, {"q","exit"}, { "fuckoff", "exit" }, { "spierdalaj", "exit" },
                {"cls","cls"},
                { "help", "help" },
                { "selftest", "selftest" }
            };
        }

        /// <summary>
        /// A hook to set up extra commands or replace the default mapping
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="overwrite"></param>
        public void SetUpCommandMap(Dictionary<string, string> commands, bool overwrite = false)
        {
            if (overwrite)
            {
                _commandMap = commands ?? new Dictionary<string, string>();
            }
            else
            {
                foreach (var key in commands.Keys)
                {
                    _commandMap[key] = commands[key];
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
                Handle_Unknown(command);
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

            
            try
            {
                var args = command.Split(' ');

                outCommand = _commandMap[args[0].ToLower()];
                
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
            catch
            {
                //ignore
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

        protected virtual void Handle_Unknown(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                ConsoleEx.WriteLine($"Sorry mate, but '{command}' is not something I recognise...", ConsoleColor.DarkRed);
                Console.WriteLine();
            }
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

                return;
            }

            Console.WriteLine("This is a selftest command output. The command is registered properly!");
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
        /// Prints currently supported commands
        /// </summary>
        protected virtual void PrintCommands()
        {
            var t = this.GetType();
            var methods =
                t.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                             .Where(m => m.Name.ToLower().StartsWith("handle_") && !new[] { "handle_unknown", "handle_selftest"}.Contains(m.Name.ToLower()))
                             .Select(m => m.Name.ToLower().Replace("handle_", ""));

            Console.WriteLine("Supported commands are:");
            foreach (var method in methods)
            {
                var aliases = _commandMap.Where(cm => cm.Value == method && cm.Key != method).Select(cm => cm.Key).ToList();

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
                    if (bool.TryParse(stringPValue, out boolValue))
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
