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
        /*
         * Класс работника, который параллельно ищет подстроку в списке файлов
         * Получает список файлов, искомую подстроку и callback (делегат), который нужно выполнить по завершению работы
         * (иначе не отследить, когда потоки завершили работу).
         * 
         * Метод Run кладет список файлов в ThreadPool, и кладет делегат finder для обработки этих файлов
         * А ThreadPool запускает параллельно на нескольких ядрах этот делегат и передает ему файлы
         * Когда ThreadPool выполнит все задачи, которые мы ему дали - непонятно, поэтому отслеживать нужно вручную
         * Делегат в конце работы уменьшает количество невыполненных заданий на 1, и если невыполненных заданий = 0, 
         * то все задания выполнены, значит можно вызвать callback, который нужно выполнить по завершению работы.
         * А в callback'e мы снаружи указываем, что нужно сделать после поиска файлов (в данном случае мы в callback'е просто копируем результат в task)
         * */
        class FinderWorker
        {
            TaskCompletionSource<List<FileInfo>> task;//Метод, выполняемый по окончанию задания

            int remainCount;//Количество невыполненных заданий (нужно для отслеживания, во всех ли файлах работник искал подстроку)

            FileInfo[] fileInfoArr; //Массив файлов, в которых нужно искать подстроку

            public ConcurrentBag<FileInfo> Result; //Аналог List<FileInfo>, только потокобезопасный и неупорядоченный

            string searchString;//Искомая подстрока

            public FinderWorker(FileInfo[] fileInfoArr, string searchString, TaskCompletionSource<List<FileInfo>> task)
            {
                this.task = task;
                this.fileInfoArr = fileInfoArr;
                remainCount = fileInfoArr.Length;//Количество невыполненных задач = количество полученных для поиска файлов
                this.searchString = searchString;
                Result = new ConcurrentBag<FileInfo>();
            }
            
            public void Run()//Стартуем
            {
                foreach (FileInfo fileinfo in fileInfoArr)//Напихать ThreadPool задачами
                {
                    //Положить в ThreadPool метод finder, и объект fileinfo, и ThreadPool в несколько потоков одновременно будет вызывать finder(fileinfo);
                    ThreadPool.QueueUserWorkItem(finder, fileinfo);
                }
            }

            //Метод, который собственно ищет подстроку в файле
            public void finder(Object inputFile)
            {
                /*
                //Считает количество запушенных потоков.
                //Ну типа ThreadPool вызывает finder, а finder смотрит, сколько других finder'ов одновременно с ним работают
                int worker = 0;
                int io = 0;
                ThreadPool.GetAvailableThreads(out worker, out io);
                Console.WriteLine(worker.ToString()+io.ToString());
                */

                if (inputFile is FileInfo)//Чисто на всякий случай проверка
                {
                    FileInfo file = (FileInfo)inputFile;//Преобразовать тип inputFile из Object в FileInfo
                    try
                    {
                        //Если файл не слишком большой и в нем содержится искомая подстрока
                        if (file.Length<1024*1024*10 && File.ReadAllText(file.FullName).Contains(searchString))
                        {
                            //Добавить этот файл в список результатов
                            Result.Add((FileInfo)inputFile);
                        }
                    }
                    catch (Exception e)
                    {
                        //Нормальные люди вызывают callback(e), типа ошибка, но мы просто выведем сообщение в консоль
                        Console.WriteLine(e.Message);
                    }
                   
                }

                Interlocked.Decrement(ref remainCount);//Уменьшить количество невыполненных заданий
                if (remainCount <= 0){//Если все выполнено
                    //Вызывает callback с результатом. 
                    //callback(false, Result.ToList<FileInfo>());//false - потому что нет ошибки, Result.ToList() - преобразовывает ConcurrentBag в обычный List
                    task.SetResult(Result.ToList<FileInfo>());
                    //Воркер закончил работу, дальше 
                }
            }
        }

        /*
         * Самая главная функция, которую нужно вызывать для поиска. 
         * Кстати, она почит асинхронная, она почти сразу возвращает Task
         * Она запускаeт FinderWorker и возвращает Task, в который потом FinderWorker положит результат
         * */
        public static Task<List<FileInfo>> Find(string startPath, string searchMask, string searchString)
        {
            Console.WriteLine("Find called");

            //Создать управляемый task (мы сами решаем, когда он готов, в отличии от обычного Task)
            TaskCompletionSource<List<FileInfo>> task = new TaskCompletionSource<List<FileInfo>>();

            //Получить информацию о папке, в которой ишем файлы, и получить оттуда все файлы, соответствующие маске
            FileInfo[] fileInfoArr = new DirectoryInfo(startPath).GetFiles(searchMask, SearchOption.AllDirectories);

            Console.WriteLine("List files to find created "+ fileInfoArr.Length.ToString());

            //Если нашли хоть один файл, соответствующий маске
            if (fileInfoArr.Length > 0)
            {
                //Создать новый FinderWorker
                FinderWorker finderWorker = new FinderWorker(
                    fileInfoArr,//Список файлов для поиска в них подстроки
                    searchString,//Подстрока
                    /*
                    (bool err, List<FileInfo> result) => {//Callback, вызываемый по завершению поиска файлов. первый параметр err, потому что так правильней, хоть здесь можно и без него.
                        task.SetResult(result);//Установить результат в управляемый task, чтобы c# знал, что асинхронный код завершил работу
                    }
                    */
                    task
                );

                //Установить в ThreadPool максимальное количество потоков равное количеству ядер процессора
                ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

                //Запустить findWorker
                finderWorker.Run();
            }
            else
            {
                //Установить пустой список файлов в качестве результата в управляемый task
                task.SetResult(new List<FileInfo>());
            }

            //Вернуть обычный Task, который мы берем из управляемого TaskCompletionSource task'а
            return task.Task;
        }
    }
}
