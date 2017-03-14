using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindLibrary
{
    public static class FileFind
    {
        class FinderWorker
        {
            Action<bool, List<FileInfo>> callback;//Метод, выполняемый по окончанию задания

            int remainCount;//Количество невыполненных заданий

            FileInfo[] fileInfoArr;

            public ConcurrentBag<FileInfo> Result;

            string searchString;

            public FinderWorker(FileInfo[] fileInfoArr, string searchString, Action<bool, List<FileInfo>> callback)
            {
                this.callback = callback;
                this.fileInfoArr = fileInfoArr;
                remainCount = fileInfoArr.Length;
                this.searchString = searchString;
                Result = new ConcurrentBag<FileInfo>();
            }
             
            public void Run()
            {
                foreach (FileInfo fileinfo in fileInfoArr)//Напихать ThreadPool задачами
                {
                    ThreadPool.QueueUserWorkItem(finder, fileinfo);
                }
            }

            public void finder(Object inputFile)
            {
                /*
                //Считает количество запушенных потоков
                int worker = 0;
                int io = 0;
                ThreadPool.GetAvailableThreads(out worker, out io);
                Console.WriteLine(worker.ToString()+io.ToString());
                */

                if (inputFile is FileInfo)
                {
                    FileInfo file = (FileInfo)inputFile;
                    try
                    {
                        if (file.Length<1024*1024*10 && File.ReadAllText(file.FullName).Contains(searchString))
                        {
                            Result.Add((FileInfo)inputFile);
                        }
                    }
                    catch
                    {

                    }
                   
                }

                Interlocked.Decrement(ref remainCount);//Уменьшает количество невыполненных заданий
                if (remainCount <= 0){//Если все выполнено
                    callback(false, Result.ToList<FileInfo>());//Вызывает callback с результатом
                }
            }
        }


        public static Task<List<FileInfo>> Find(string startPath, string searchMask, string searchString)
        {
            Console.WriteLine("Find called");

            TaskCompletionSource<List<FileInfo>> task = new TaskCompletionSource<List<FileInfo>>();

            FileInfo[] fileInfoArr = new DirectoryInfo(startPath).GetFiles(searchMask, SearchOption.AllDirectories);

            Console.WriteLine("List files to find created "+ fileInfoArr.Length.ToString());

            if (fileInfoArr.Length > 0)
            {
                FinderWorker finderWorker = new FinderWorker(
                    fileInfoArr,
                    searchString,
                    (bool err, List<FileInfo> result) => {//Callback, вызываемый по завершению поиска файлов
                        task.SetResult(result);
                    }
                );

                //ConcurrentQueue<FileInfo> fileInfoQueue = new ConcurrentQueue<FileInfo>(fileInfoArr);
                ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

                finderWorker.Run();
            }
            else
            {
                task.SetResult(new List<FileInfo>());
            }
           

            return task.Task;
        }
    }
}
