using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdUtils
{
    /// <summary>
    /// Cmd Watcher monitors the cmd user input and redirects it to ICmdCommandHandler for further processing
    /// </summary>
    public class CmdWatcher
    {
        /// <summary>
        /// string builder that handles command input
        /// </summary>
        private StringBuilder sb { get; set; }

        /// <summary>
        /// Command caching list
        /// </summary>
        private List<string> commandCache { get; set; }

        /// <summary>
        /// Index of a command in the cache; used to browse through commands with up and down arrows
        /// </summary>
        private int? commandCacheIdx { get; set; }

        /// <summary>
        /// Default Prompt
        /// </summary>
        private static readonly string defaultPrompt = "cmd>";


        private string _prompt;

        /// <summary>
        /// The actually set Prompt
        /// </summary>
        public string Prompt {
            get
            {
                return _prompt;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _prompt = value;
                }
            }
        }

        /// <summary>
        /// Console color for Prompt string
        /// </summary>
        public ConsoleColor PromptColor { get; set; }
         

        /// <summary>
        /// Cmd handler instance
        /// </summary>
        private ICmdCommandHandler cmdHandler { get; set; }


        /// <summary>
        /// Creates a new instance with a specified command handler and prompt string
        /// </summary>
        /// <param name="cmdHandler"></param>
        /// <param name="prompt"></param>
        public CmdWatcher(ICmdCommandHandler cmdHandler, string prompt = null)
        {
            if (cmdHandler == null)
            {
                throw new ArgumentNullException("CmdCommandHandler cannot be null!");
            }

            this.cmdHandler = cmdHandler;
                        
            this.Prompt = prompt ?? defaultPrompt;
        }


        /// <summary>
        /// Creates a new instance with a specified command handler
        /// </summary>
        /// <param name="cmdWatcher"></param>
        public CmdWatcher(ICmdCommandHandler cmdHandler)
            : this(cmdHandler, defaultPrompt)
        { }

        /// <summary>
        /// Creates a new instance with the default Cmd handler
        /// </summary>
        public CmdWatcher()
            //just use the default cmd handler!
            : this(new DefaultCmdCommandHandler())
        { }


        /// <summary>
        /// Initiate Cmd watcher - passes the control to the watcher. Only when ICommandHandler reports exit, the watcher also exits
        /// </summary>
        public void Init()
        {
            //reset string builder if this watcher is to be reused.
            sb = new StringBuilder();
            //and do the same to the commands cache
            commandCache = new List<string>();

            cmdHandler.PrintHandlerInfo();

            PrintPrompt();

            while (!cmdHandler.Exit())
            {
                HandleInput();
            }
        }

        /// <summary>
        /// Handles user input
        /// </summary>
        private void HandleInput()
        {
            var cki = Console.ReadKey(false);

            //should only react to 'real' chars and ignore non char characters
            //so need to test unicode category
            var ucc = Char.GetUnicodeCategory(cki.KeyChar);


            //ignore control chars
            if (ucc != System.Globalization.UnicodeCategory.Control)
            {
                int cursorPosition = Console.CursorLeft;

                if (cursorPosition != GetPromptAndCommandLength() + 1)
                {
                    sb.Insert(cursorPosition - GetPromptLength() - 1, cki.KeyChar);
                }
                else
                {
                    sb.Append(cki.KeyChar);
                }

                PrintWithPrompt(cursorPosition: cursorPosition);
            }
            else
            {
                //it was a control char...

                //what happens here is that with each key press, console cursor moves right,
                //so just need to move it one step back

                switch (cki.Key)
                {
                    case ConsoleKey.Backspace:
                        HandleBackspace();
                        break;

                    case ConsoleKey.Enter:
                        HandleEnter();
                        break;

                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow();
                        break;

                    case ConsoleKey.RightArrow:
                        HandleRightArrow();
                        break;

                    //handle up/down arrows to browse through command cache
                    case ConsoleKey.DownArrow:
                        HandleDownArrow();
                        break;

                    case ConsoleKey.UpArrow:
                        HandleUpArrow();
                        break;

                    //handle Esc to reset input
                    case ConsoleKey.Escape:
                        HandleEscape();
                        break;

                    default:
                        //since with each key press the cursor goes one step right, just move it back
                        //but reprint as it looks like the char at cursor position gets wiped out
                        PrintWithPrompt(cursorPosition: Console.CursorLeft -= 1);
                        break;
                }
            }
        }

        /// <summary>
        /// Handles down arrow - browses from older to newer cached command if possible
        /// </summary>
        private void HandleDownArrow()
        {
            //work out the index of a command typed after the one the idx points to currently
            if (commandCacheIdx.HasValue)
            {
                if (commandCacheIdx + 1 < commandCache.Count)
                {
                    commandCacheIdx += 1;
                }
                else
                {
                    commandCacheIdx = commandCache.Count - 1;
                }

                //and print it
                PrintCachedCommand(commandCacheIdx);
            }
            else
            {
                //each key press moves the cursor right so need to move it back
                Console.CursorLeft -= 1;
            }
        }

        /// <summary>
        /// Handles up arrow - browses from newer to older cached command if possible
        /// </summary>
        private void HandleUpArrow()
        {
            if (commandCacheIdx.HasValue)
            {
                if (commandCacheIdx - 1 >= 0)
                {
                    commandCacheIdx -= 1;
                }
                else
                {
                    commandCacheIdx = 0;
                }
            }
            else
            {
                //if there is cache set the idx to the last cmd
                if (commandCache.Count > 0)
                {
                    commandCacheIdx = commandCache.Count - 1;
                }
                //otherwise handle the cursor postion properly
                else
                {
                    //each key press moves the cursor right so need to move it back
                    Console.CursorLeft -= 1;
                }
            }

            //and print it
            PrintCachedCommand(commandCacheIdx);
        }

        /// <summary>
        /// Handkles escape key - wipes out the input
        /// </summary>
        private void HandleEscape()
        {
            int currentLength = sb.Length + 1; //so the esc char is also wiped out
            sb.Clear();
            PrintWithPrompt(currentLength);
        }

        /// <summary>
        /// Handles left arrow
        /// </summary>
        private void HandleRightArrow()
        {
            int cursorPosition = Console.CursorLeft;
            if (cursorPosition > GetPromptAndCommandLength())
            {
                cursorPosition = GetPromptAndCommandLength();
            }
            //With each key press carret goes one step ahead, so no need to move it further right!
            PrintWithPrompt(cursorPosition: cursorPosition);
        }


        /// <summary>
        /// Handles right arrow
        /// </summary>
        private void HandleLeftArrow()
        {
            int cursorPosition = Console.CursorLeft;
            if (cursorPosition <= GetPromptLength() + 1)
            {
                cursorPosition = GetPromptLength();
            }
            else
            {
                //with every key press the carret goes one step ahead, hence need to go one step back!
                cursorPosition -= 2;
            }
            PrintWithPrompt(cursorPosition: cursorPosition);
        }

        /// <summary>
        /// Handles backspace
        /// </summary>
        private void HandleBackspace()
        {
            //remove a char
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            PrintWithPrompt(1);
        }
                

        /// <summary>
        /// Handles enter
        /// </summary>
        private void HandleEnter()
        {
            var command = sb.ToString();

            //cache the executing command
            if (!string.IsNullOrWhiteSpace(command) && (commandCache.Count == 0 || commandCache[commandCache.Count - 1] != command))
            {
                commandCache.Add(command);
            }

            ResetPreviousCommandCacheIndex();

            //finalise the line with Prompt and command as it was...
            Console.Write(Environment.NewLine);

            //and redirect the command handling to the cmd handler
            cmdHandler.HandleCommand(command);

            //check if should continue watching cmd or can quit
            if (!cmdHandler.Exit())
            {
                //wipe out string builder
                sb.Clear();
                
                PrintPrompt();
            }
        }

        /// <summary>
        /// Resets current command cache indexer used to scroll through the previous commands
        /// </summary>
        private void ResetPreviousCommandCacheIndex()
        {
            //reset the commandCache idx
            commandCacheIdx = null;
        }


        /// <summary>
        /// Prints cached command
        /// </summary>
        private void PrintCachedCommand(int? idx)
        {
            if (idx.HasValue && idx >= 0 && idx < commandCache.Count)
            {
                //grab the cached command
                var newCmd = commandCache[(int)idx];

                //work out if leaning the console after the new command is required
                var clear = 0;
                if (newCmd.Length < sb.Length)
                {
                    clear = sb.Length - newCmd.Length;
                }

                //set new cmd on the string builder
                sb.Clear();
                sb.Append(newCmd);

                //and print it
                PrintWithPrompt(clear);
            }
        }


        /// <summary>
        /// Gets the length of Prompt string
        /// </summary>
        /// <returns></returns>
        private int GetPromptLength()
        {
            return Prompt.Length;
        }

        /// <summary>
        /// Gets the length of the Prompt and input command strings
        /// </summary>
        /// <returns></returns>
        private int GetPromptAndCommandLength()
        {
            return GetPromptLength() + sb.Length;
        }

        /// <summary>
        /// Prints Prompt
        /// </summary>
        private void PrintPrompt()
        {
            Console.ForegroundColor = PromptColor;
            Console.Write(Prompt);
            Console.ResetColor();
        }

        /// <summary>
        /// Prints a current command with Prompt
        /// </summary>
        /// <param name="clearAfter">How many chars after the promt + command should be cleared</param>
        /// <param name="cursorPosition">Where to place the cursor once command is printed</param>
        private void PrintWithPrompt(int clearAfter = 0, int? cursorPosition = null)
        {
            Console.CursorLeft = 0;

            PrintPrompt();
            Console.Write(sb.ToString());

            if (clearAfter > 0)
            {
                Console.Write(string.Join("", Enumerable.Repeat(" ", clearAfter).ToArray()));
                Console.CursorLeft -= clearAfter;
            }

            if (cursorPosition.HasValue)
            {
                Console.CursorLeft = (int)cursorPosition;
            }
        }
    }
}
