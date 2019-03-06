using System;
using System.IO;

namespace Mospel.MqttBroker
{

    internal class LogHelper
    {
        string FilePath = "";
        string FileName = "";
        public bool DailyFile { get; set; }

        public LogHelper(string path, string fileName, bool dailyFile = true)
        {
            FilePath = path;
            FileName = fileName;
            DailyFile = dailyFile;
        }                                                                                                                                                                                    

        public void Add(params object[] args)
        {
            try
            {
                string fName = Path.GetFileNameWithoutExtension(FileName) + (DailyFile == true ? DateTime.Today.ToString("-MM-dd-yyyy") : "") + Path.GetExtension(FileName);
                if (!Directory.Exists(FilePath))
                    Directory.CreateDirectory(FilePath);
                StreamWriter sw = new StreamWriter(new FileStream(FilePath + "\\" + fName, FileMode.Append));
                string message = "";
                foreach(object arg in args)
                {
                    if (arg != null)
                    {
                        if (message.Length > 0)
                            message += " ";
                        message += arg;
                    }
                }
                sw.WriteLine(DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt") + ": " + message);
                sw.Close();
            }
            catch (Exception ex)
            {

            }
        }
    }
}