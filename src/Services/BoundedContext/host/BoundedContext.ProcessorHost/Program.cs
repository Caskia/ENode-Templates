using System;

namespace BoundedContext.ProcessorHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var bootstrap = new Bootstrap();
            bootstrap.Start();

            Console.WriteLine("Press enter to exit...");
            var line = Console.ReadLine();
            while (line != "exit")
            {
                switch (line)
                {
                    case "cls":
                        Console.Clear();
                        break;

                    default:
                        return;
                }
                line = Console.ReadLine();
            }
        }
    }
}