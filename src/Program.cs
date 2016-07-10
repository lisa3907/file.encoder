using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileEncoder
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var _encoder = new Encoder();

            _encoder.WriteTitle();
            if (args.Length < 1 || args.Length > 5)
            {
                _encoder.WriteUsage();
                return;
            }

            try
            {
                for (int i = 0; i < args.Length; i++)
                    _encoder.ParsingParameter(args[i]);

                Console.WriteLine(String.Format("\n{0} file(s) .......", _encoder.DoEncoding()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
