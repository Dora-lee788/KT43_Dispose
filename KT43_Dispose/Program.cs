namespace KT43_Dispose
{
    class DatabaseConnection : IDisposable, IAsyncDisposable
    {
        private List<string> queryLog = new List<string>();
        private int connectionId;
        private bool disposed;

        public DatabaseConnection(int id)
        {
            connectionId = id;
            Console.WriteLine("Соединение открыто. ID: " + connectionId);
        }

        public void AddQuery(string query)
        {
            if (disposed)
                throw new ObjectDisposedException("DatabaseConnection");

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Пустой запрос");

            queryLog.Add(query);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (disposing)
            {
                queryLog.Clear();
                Console.WriteLine("Лог запросов очищен");
            }

            if (connectionId != 0)
            {
                Console.WriteLine("Нативный ресурс закрыт. ID: " + connectionId);
                connectionId = 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DatabaseConnection()
        {
            Console.WriteLine("Финализатор вызван");
            Dispose(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!disposed)
            {
                await Task.Delay(10);
                queryLog.Clear();
                Console.WriteLine("DisposeAsyncCore выполнен");
            }
        }

        class Program
        {
            static async Task Main()
            {
                while (true)
                {
                    Console.WriteLine("\n1 - using");
                    Console.WriteLine("2 - await using");
                    Console.WriteLine("3 - Финализатор");
                    Console.WriteLine("0 - Выход");
                    Console.Write("Выбор: ");

                    if (!int.TryParse(Console.ReadLine(), out int choice))
                    {
                        Console.WriteLine("Введите число");
                        continue;
                    }

                    if (choice == 0)
                        break;

                    if (choice == 1)
                        RunSync();
                    else if (choice == 2)
                        await RunAsync();
                    else if (choice == 3)
                        RunFinalizer();
                    else
                        Console.WriteLine("Неверный пункт меню");
                }
            }

            static int ReadId()
            {
                while (true)
                {
                    Console.Write("Введите ID: ");

                    if (int.TryParse(Console.ReadLine(), out int id) && id > 0)
                        return id;

                    Console.WriteLine("Введите положительное целое число");
                }
            }

            static void ReadQueries(DatabaseConnection connection)
            {
                Console.WriteLine("Введите запросы (0 - завершить):");

                while (true)
                {
                    string query = Console.ReadLine()!;

                    if (query == "0")
                        break;

                    try
                    {
                        connection.AddQuery(query);
                        Console.WriteLine("Запрос добавлен");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            static void RunSync()
            {
                using (DatabaseConnection connection = new DatabaseConnection(ReadId()))
                {
                    ReadQueries(connection);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            static async Task RunAsync()
            {
                await using (DatabaseConnection connection = new DatabaseConnection(ReadId()))
                {
                    ReadQueries(connection);
                }
            }

            static void RunFinalizer()
            {
                CreateConnection(ReadId());
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            static void CreateConnection(int id)
            {
                DatabaseConnection connection = new DatabaseConnection(id);
                connection.AddQuery("SELECT * FROM Users");
            }
        }
    }

}

