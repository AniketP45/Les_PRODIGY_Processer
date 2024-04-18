using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PRODIGY_Routine;

namespace Les_PRODIGY_Processer
{
    internal class Program
    {

        static string processor_name = Convert.ToString(ConfigurationManager.AppSettings["PROCESSOR_NAME"]) + " : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        static void Main(string[] args)
        {
            ProdigyClass rapex = new ProdigyClass();
            SetCulture(rapex);

            rapex.WriteLog("====================================");
            rapex.WriteLog(processor_name + " Process Started...");
            rapex.StartProcess();
            rapex.WriteLog(processor_name + " Process Completed...");
            rapex.WriteLog("====================================");
            Environment.Exit(0);
        }
        public static void SetCulture(ProdigyClass _Routine)
        {
            System.Globalization.CultureInfo _defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            _Routine.WriteLog("Default regional setting - " + _defaultCulture.DisplayName);
            _Routine.WriteLog("Current regional setting - " + System.Threading.Thread.CurrentThread.CurrentCulture.DisplayName);
        }
    }
}
