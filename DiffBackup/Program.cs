
namespace DiffBackup
{
    using DiffBackup.Schemas.Serialization;
    using System;

    public static class Program
    {
        public static void Main(string[] args)
        {
            FileInformation info = new FileInformation();
            info.Path = @"D:\Backup Test\Complete\cpumemory.pdf";
            Console.WriteLine("Hello, World!");
        }
    }
}
