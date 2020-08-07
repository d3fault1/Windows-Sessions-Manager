using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 6)
        {
            Console.WriteLine("Syntax Error");
            Console.WriteLine("Usage: SessionCreator.exe [domain] [user] [pass] [width] [height] [scale]");
            Console.WriteLine();
            return;
        }
        NamedPipeClientStream client = new NamedPipeClientStream(@"SessManagingPipe");
        client.Connect(1000);
        StreamWriter sw = new StreamWriter(client);
        sw.AutoFlush = true;
        for (int i = 0; i < 6; i++)
        {
            sw.WriteLine(args[i]);
            Thread.Sleep(70);
        }
        client.Close();
        client.Dispose();
        return;
    }
}
