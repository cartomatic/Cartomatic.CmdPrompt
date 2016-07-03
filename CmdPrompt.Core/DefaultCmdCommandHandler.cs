using System;
using System.Collections.Generic;
using System.Linq;
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
        public void SetUpCommandMap(Dictionary<string, string> commands, bool overwrite)
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
        public virtual void HandleCommand(string command)
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

                m.Invoke(this, p);
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
            var outCommand = "unknown";
            arguments = null;

            
            try
            {
                var args = command.Split(' ');

                outCommand = _commandMap[args[0].ToLower()];
                
                arguments = new Dictionary<string, string>();
                for (var a = 1; a < args.Length; a++)
                {
                    var paramParts = args[a].Split(':');
                    if (paramParts.Length > 0)
                    {
                        var pName = paramParts[0].ToLower();

                        arguments[pName] = paramParts.Length == 2 ? paramParts[1] : string.Empty;
                    }
                }
            }
            catch
            {
                //ignore
            }

            return outCommand;
        }

        /// <summary>
        /// Justg a test command
        /// </summary>
        /// <param name="args"></param>
        /// <param name="help"></param>
        protected virtual void Handle_SelfTest(IDictionary<string, string> args)
        {
            var help = args.Keys.Any(k => k.ToLower() == "help");
            if (help)

            {
                Console.WriteLine("This is a selftest help. The command is registered properly");

                return;
            }

            Console.WriteLine("This is a selftest command output. The command is registered properly.");
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
                var aliases = _commandMap.Where(cm => cm.Value == method).Select(cm => cm.Key).ToList();

                ConsoleEx.Write(method, ConsoleColor.DarkMagenta);

                ConsoleEx.Write(aliases.Count > 0 ? "; aliases: " + string.Join(", ", aliases) : "", ConsoleColor.DarkBlue);

                Console.Write(Environment.NewLine);
            }
            Console.WriteLine();

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
            ConsoleEx.WriteLine($"Sorry mate, but '{command}' is not something I recognise...", ConsoleColor.DarkRed);
            Console.WriteLine();
        }

    }
}
