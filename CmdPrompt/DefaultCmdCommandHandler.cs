using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdUtils
{
    /// <summary>
    /// Default command handler
    /// </summary>
    public class DefaultCmdCommandHandler : ICmdCommandHandler
    {
        /// <summary>
        /// whether or not the handler's exit command has been called
        /// </summary>
        bool exit = false;

        /// <summary>
        /// Command map used to manage command aliases
        /// </summary>
        Dictionary<string, string> commandMap;

        /// <summary>
        /// ccreates an instance
        /// </summary>
        public DefaultCmdCommandHandler()
        {
            SetUpCommandMap();
        }

        /// <summary>
        /// Sets up the command map
        /// </summary>
        protected void SetUpCommandMap()
        {
            commandMap = new Dictionary<string, string>()
            {
                {"exit","exit"}, {"e","exit"}, {"quit","exit"}, {"q","exit"},
                {"cls","cls"}
            };
        }

        /// <summary>
        /// Prints handler specific info; used to give some init info like cmd version and stuff
        /// </summary>
        public virtual void PrintHandlerInfo()
        {
            Console.WriteLine("Default cmd handler... v 1.0.0");
            Console.WriteLine();
        }

        /// <summary>
        /// Returns the state of the exit flag; client should exit if true
        /// </summary>
        /// <returns></returns>
        public virtual bool Exit()
        {
            return exit;
        }

        /// <summary>
        /// Handles a cmd command
        /// </summary>
        /// <param name="command"></param>
        public virtual void HandleCommand(string command)
        {
            var nc = NormaliseCommand(command);
            
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
                    p = new object[] { command };
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
        /// <returns></returns>
        protected virtual string NormaliseCommand(string command)
        {
            string outCommand = "unknown";
            try
            {
                outCommand = commandMap[command.ToLower()];
            }
            catch { }
            
            return outCommand;
        }

        /// <summary>
        /// Handles exit command
        /// </summary>
        protected virtual void Handle_Exit()
        {
            Console.WriteLine("Bye, bye...");
            Console.WriteLine();
            exit = true;
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
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Sorry mate, but '{0}' is not something I recognise...", command);
            Console.ResetColor();
            Console.WriteLine();
        }

    }
}
