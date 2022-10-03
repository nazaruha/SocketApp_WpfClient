using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleServer
{
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
        static int count = 1;
        public static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 2854);
            server.Start();
            Console.WriteLine("Server started");
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                lock (_lock) list_clients.Add(count, client);
                Console.WriteLine("Client #{0} connected", count);
                Thread thread = new Thread(handle_client);
                thread.Start(count);
                count++;
            }
        }

        private static void handle_client(object count)
        {
            int id = (int)count;
            TcpClient client;
            lock(_lock) client = list_clients[id];
            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byte_count = stream.Read(buffer, 0, buffer.Length);
                if (byte_count == 0) break; // це начебто перевіряє чи клієнт більше нічого не відправляє (від'єднався)
                string data = Encoding.UTF8.GetString(buffer, 0, byte_count);
                broadcast(data, id);
                Console.WriteLine($"id#{id} :: " + data);
            }
            remove_client(id, client);
        }
        private static void remove_client(int id, TcpClient client)
        {
            lock (_lock) list_clients.Remove(id);
            Console.WriteLine("id#{0} has been deleted", id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private static void broadcast(string data, int id)
        {
            string msg = $"id#{id} :: {data}";
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            lock (_lock)
            {
                TcpClient client = list_clients[id];
                NetworkStream stream = client.GetStream(); // отримує потік самого клієнта
                stream.Write(buffer, 0, buffer.Length); // кидає повідомлення клієнту
            }
        }
    }
}