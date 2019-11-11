using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Reflection;

namespace ComputerShutdown
{
    class Program
    {
        private const string URL = "https://hubert.jmdev.ca/schedule";
        //private string urlParameters = "?api_key=123";

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

        private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        public class DataObject
        {
            public int day_int { get; set; }
            public string day_string { get; set; }
            public int start { get; set; }
            public int end { get; set; }
        }


        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        static void Main(string[] args)
        {
            AddApplicationToStartup();
            cancelShutdown();
            Console.WriteLine("Do Not Close!");
            int timeout = 0;
            String day = "";

            int startTemp = 0;
            int endTemp = 0;

            while (true)
            {
                //Sleep before looping again
                timeout = (60 - DateTime.Now.Second) * 1000 - DateTime.Now.Millisecond;

                //Day of week
                day = DateTime.Today.DayOfWeek.ToString();

                // Register the handler
                SetConsoleCtrlHandler(Handler, true);

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URL);

                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // List data response.
                HttpResponseMessage response = client.GetAsync(URL).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
                if (response.IsSuccessStatusCode)
                {
                    // Parse the response body.
                    var dataObjects = response.Content.ReadAsAsync<IEnumerable<DataObject>>().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                    foreach (var d in dataObjects)
                    {
                       
                        if (d.day_string == day)
                        {
                            if(d.start != startTemp || d.end != endTemp)
                            {
                                if (startTemp == 0 && endTemp == 0)
                                {
                                    Console.WriteLine("Today's time: {0}-{1}", d.start, d.end);
                                }
                                else
                                {
                                    Console.WriteLine("Time have been updated to {0}-{1}", d.start, d.end);
                                }

                                DateTime start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, d.start, 0, 0);
                                DateTime end = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, d.end, 0, 0);

                                if (DateTime.Now > end || DateTime.Now < start)
                                {                             
                                    shutdownComputer();
                                }
                                startTemp = d.start;
                                endTemp = d.end;
                            }
                            
                            
                        }
                    }
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }

                client.Dispose();

                Thread.Sleep(timeout);
            }
        }

        /**
         * Adding application process to startup 
         */
        public static void AddApplicationToStartup()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.SetValue("ComputerShutdown", System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        /**
         *  Application close event handler
         */
        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    shutdownComputer();
                    // TODO Cleanup resources
                    Environment.Exit(0);
                    return false;

                default:
                    return false;
            }
        }

        /**
         * Shutdown the computer (Gives 60 seconds to the user)
         */
        private static void shutdownComputer()
        {
            Console.WriteLine("It is time to go sleep! Good night! <3");
            System.Diagnostics.Process.Start("shutdown", "/s /t 60");
        }

        /**
         *  Cancel the shutdown
         */
        private static void cancelShutdown()
        {
            Console.WriteLine("The process of shuting down the computer have been canceled!");
            System.Diagnostics.Process.Start("shutdown", "/a");
        }
    }
}
