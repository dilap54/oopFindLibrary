using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using FindLibrary;

namespace FindProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            run();
        }
        static void run()
        {
            Console.WriteLine("Program start");
            List<FileInfo> fileInfoList = FileFind.Find("C:\\Users\\miniARVES\\Documents\\Cloud\\Учёба\\4 семестр\\ООП\\Задачи\\FindLibrary", "*", "cs").Result;
            foreach (FileInfo fileInfo in fileInfoList)
            {
                Console.WriteLine(fileInfo.FullName);
            }
            Console.WriteLine("Program end");
            Console.ReadKey();
        }
    }
}
