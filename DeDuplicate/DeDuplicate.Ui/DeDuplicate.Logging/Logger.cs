using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeDuplicate.Logging
{
    public class Logger
    {
        private string _filePath;

        public Logger()
        {
            this._filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\dedupe.txt";
        }

        public void Write(string orignal, string destination, string other)
        {
            using (var sw = new StreamWriter(this._filePath,true))
            {
                sw.WriteLine(orignal + " " + destination + "");

                if (!String.IsNullOrEmpty(other))
                    sw.WriteLine(other);

                sw.WriteLine(" ");
            }
        }
    }

}
