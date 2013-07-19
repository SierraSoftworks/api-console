using API_Console.AST;
using API_Console.Controllers.Help;
using API_Console.DLR;
using Irony.Parsing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Expr = System.Linq.Expressions.Expression;

namespace API_Console
{
    class Engine
    {
        public Engine()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            RegisterBuiltIns(BuiltInFunctions as IDictionary<string, object>);
        }

        void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            if (!CancelStack.Any() && string.IsNullOrWhiteSpace(CurrentInput))
            {
                using (new ConsoleForeground(ConsoleColor.Red))
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write("Exit ");
                }
                Console.WriteLine("type 'exit' or 'quit'");
                PrintPrompt();
            }
            else if (!string.IsNullOrWhiteSpace(CurrentInput))
            {
                CurrentInput = "";
            }
            else
                CancelStack.Pop()();
        }

        #region Variables

        /// <summary>
        /// Holds a stack of functions which can be used
        /// to handle a Ctrl+C cancel command.
        /// </summary>
        private readonly Stack<Action> CancelStack = new Stack<Action>();

        /// <summary>
        /// The RestSharp client to use for making requests
        /// </summary>
        private IRestClient Client = new RestClient();

        private bool Exit = false;

        readonly object NoInput = new object();
        public readonly object NoOutput = new object();

        #endregion

        #region External Methods

        public void Run()
        {
            CancelStack.Clear();

            while (!Exit)
            {
                PrintPrompt();
                var line = Console.ReadLine();
                try
                {
                    var result = ParseLine(line);


                    if (result == NoInput)
                    {
                        CurrentInput = "";
                        continue;
                    }

                    if (result == NoOutput)                    
                        continue;
                    

                    if (result == GetMemberBinder.MissingMember)
                    {
                        using (new ConsoleForeground(ConsoleColor.Red))
                            Console.WriteLine("Command Not Found");
                        continue;
                    }

                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.White;

                    var formatted = FormatObject(result);
                    if (formatted.IndexOf("\n") == -1)
                        Console.Write(" => ");
                    Console.WriteLine(formatted);
                    Console.ForegroundColor = color;

                }
                catch (Exception ex)
                {
                    CurrentInput = "";
                    using (new ConsoleForeground(ConsoleColor.Red))
                    {
                        var e = ex.InnerException;
                        while (e != null)
                        {
                            Console.WriteLine(e.Message);
                            e = e.InnerException;
                        }
                    }
                }
            }
        }

        #endregion

        #region Built-in Methods

        internal void UpdateClient(Action<IRestClient> updater)
        {
            updater(Client);
        }

        private Dictionary<string, Action<IRestRequest>> PreProcessors = new Dictionary<string, Action<IRestRequest>>();
        internal void RegisterPreprocessor(string key, Action<IRestRequest> preprocessor)
        {
            PreProcessors.Add(key, preprocessor);
        }

        internal void UnregisterPreprocessor(string key)
        {
            PreProcessors.Remove(key);
        }

        internal IEnumerable<string> Preprocessors
        {
            get
            {
                return PreProcessors.Keys;
            }
        }

        internal void BeginRequest(IRestRequest request)
        {
            _canAddCancelHandler = false;
            foreach (var preprocessor in PreProcessors)
                preprocessor.Value(request);

            try
            {
                var execHandle = Client.ExecuteAsync(request, OnRequestCompleted);
                CancelStack.Push(() =>
                {
                    _canAddCancelHandler = true;
                    execHandle.Abort();
                });
            }
            catch (UriFormatException)
            {
                throw new UriFormatException("Invalid URI, you might not have selected a server.");
            }
        }

        private bool _canAddCancelHandler = true;
        internal void AddCancelHandler(Action onCancel)
        {
            if (!_canAddCancelHandler)
                throw new InvalidOperationException("Cannot add a cancel handler after a request has been submitted");
            CancelStack.Push(onCancel);
        }

        internal void RemoveCancelHandler()
        {
            if (!_canAddCancelHandler)
                throw new InvalidOperationException("Cannot add a cancel handler after a request has been submitted");
            CancelStack.Pop();
        }

        #endregion

        #region Internal Methods

        private void OnRequestCompleted(IRestResponse response)
        {
            CancelStack.Pop();
            _canAddCancelHandler = true;

            bool success = response.StatusCode == HttpStatusCode.OK;

            Console.SetCursorPosition(0, Console.CursorTop);
            if (success)
            {
                using (new ConsoleForeground(ConsoleColor.Green))
                    Console.WriteLine("Request Completed: {0} {1}", (int)response.StatusCode, response.StatusDescription);
            }
            else
            {
                using (new ConsoleForeground(ConsoleColor.Red))
                    Console.WriteLine("Request Failed: {0} {1}", (int)response.StatusCode, response.StatusDescription);
            }
            using (new ConsoleForeground(ConsoleColor.White))
            {
                if (response.ContentType == "application/json")
                    Console.WriteLine(ReformatObject(response.Content));
                else
                    Console.WriteLine(response.Content);
            }

            PrintPrompt();
        }

        private void PrintPrompt()
        {
            if (string.IsNullOrWhiteSpace(CurrentInput))
                Console.Write("> ");
            else
                Console.Write(">> ");
        }

        private string ReformatObject(string property)
        {
            return FormatObject(JsonConvert.DeserializeObject(property));
        }

        private string FormatObject(object property)
        {
            return JsonConvert.SerializeObject(
                property, Formatting.Indented,
                new JsonConverter[] { new StringEnumConverter() });
        }

        private void RegisterBuiltIns(IDictionary<string, object> builtIns)
        {
            builtIns.Add("echo", new Func<object, object>(x =>
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine("{0}", x);
                PrintPrompt();
                return NoOutput;
            }));
            builtIns.Add("quit", new Func<object>(() =>
            {
                Exit = true;
                return NoOutput;
            }));
            builtIns.Add("exit", new Func<object>(() =>
            {
                Exit = true;
                return NoOutput;
            }));

            builtIns.Add("clear", new Func<object>(() =>
            {
                Console.Clear();
                return NoOutput;
            }));

            builtIns.Add("help", new Func<string, object>(controller =>
            {
                using (new ConsoleForeground(ConsoleColor.White))
                {
                    if (controller == null)
                    {
                        foreach (var key in Controllers.Keys)
                        {
                            Console.WriteLine(key);

                            try
                            {
                                foreach (var fn in ProvidedFunction.GetFunctions(Controllers[key] as IDictionary<string, object>))
                                {
                                    Console.Write("   ");
                                    Console.Write(fn.Name);

                                    if (fn.Parameters.Length > 0)
                                        using (new ConsoleForeground(ConsoleColor.Gray)) Console.Write(" (" + fn.Parameters.Aggregate((x, y) => x + ", " + y) + ")");
                                    Console.WriteLine();
                                }
                            }
                            catch
                            {

                            }
                        }
                        return NoOutput;
                    }

                    try
                    {
                        foreach (var fn in ProvidedFunction.GetFunctions(Controllers[controller] as IDictionary<string, object>, true))
                        {
                            Console.WriteLine(fn.Name);

                            if (fn.Parameters.Length > 0)
                                using (new ConsoleForeground(ConsoleColor.Gray))
                                {
                                    foreach (var p in fn.Parameters)
                                        Console.WriteLine("    {0}", p);
                                }
                            Console.WriteLine();
                        }
                    }
                    catch
                    {
                        throw new Exception("Controller '" + controller + "' not found");
                    }
                }
                return NoOutput;
            }));
        }

        #endregion

        #region Scripting Parsers

        Dictionary<string, object> Controllers = new Dictionary<string, object>();
        System.Dynamic.ExpandoObject BuiltInFunctions = new System.Dynamic.ExpandoObject();
        Parser LanguageParser = null;

        string CurrentInput = "";

        public void CompileParser()
        {
            LanguageParser = new Parser(new Language(BuiltInFunctions, Controllers));
        }

        public void RegisterController(string name, Controllers.ControllerBase controller)
        {
            Controllers.Add(name, controller.API);
        }

        private object ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return NoInput;
            if (LanguageParser == null)
                CompileParser();

            CurrentInput += line + Environment.NewLine;

            var parsed = LanguageParser.Parse(CurrentInput);
            if (parsed.HasErrors() && parsed.ParserMessages.Any(x => x.ParserState != null && x.ParserState.ExpectedTerminals.Any()))
            {
                return NoOutput;
            }
            else if (parsed.HasErrors())
            {
                using (new ConsoleForeground(ConsoleColor.Red))
                    Console.Write("Compiler Error ");
                Console.WriteLine("Please check your command and try again");

                foreach (var m in parsed.ParserMessages.Where(x => x.Level == Irony.ErrorLevel.Error))
                    Console.WriteLine("{0} {1}", m.Location.ToUiString(), m.Message);

                CurrentInput = "";

                return NoOutput;
            }

            CurrentInput = "";

            var processed = parsed.Root.AstNode as LambdaExpression;

            var compiled = processed.Compile();

            return compiled.DynamicInvoke();
        }

        #endregion

    }
}
