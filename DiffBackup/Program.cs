
namespace DiffBackup
{
    using DiffBackup.Schemas;
    using System;

    public static class Program
    {
        public static void Main(string[] args)
        {
            FileInformation info = new FileInformation();
            info.Path = @"F:\Data\File.txt";
            Console.WriteLine("Hello, World!");
        }
    }
}
