using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class MasterNode
{
    static string currentWord = "00000000";
    static object lockObj = new object();
    static bool found = false;
    static ConcurrentDictionary<string, int> workerStats = new ConcurrentDictionary<string, int>();

    static string secretPassword = "99999999";

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Coordinator started. Waiting for workers...");

        Thread statThread = new Thread(PrintWorkerStats);
        statThread.Start();

        while (!found)
        {
            TcpClient client = server.AcceptTcpClient();
            string workerIP = client.Client.RemoteEndPoint.ToString();
            workerStats[workerIP] = 0;
            Console.WriteLine($"New worker connected: {workerIP}");

            Thread workerThread = new Thread(() => HandleWorker(client, workerIP));
            workerThread.Start();
        }

        server.Stop();
        Console.WriteLine("Coordinator closed");
    }

    static void HandleWorker(TcpClient client, string workerIP)
    {
        NetworkStream stream = client.GetStream();

        // İlk bağlantıda workerlara şifreyi gönder
        byte[] passData = Encoding.UTF8.GetBytes("SECRET:" + secretPassword);
        stream.Write(passData, 0, passData.Length);
        Console.WriteLine($"Password sent to worker: {workerIP}");
        while (!found)
        {
            string word;
            lock (lockObj) // Kilit mekanizması
            {
                word = currentWord;
                currentWord = IncrementWord(currentWord);
            }

            byte[] data = Encoding.UTF8.GetBytes(word);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[256];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("FOUND:"))
            {
                Console.WriteLine("Password found: " + response.Substring(6));
                found = true;
                break;
            }
            else
            {
                workerStats[workerIP]++;
            }
        }
        client.Close();
    }

    static void PrintWorkerStats()
    {
        while (!found)
        {
            Thread.Sleep(1000);

            Console.WriteLine("\n--- Worker Performance Report ---");
            foreach (var worker in workerStats)
            {
                Console.WriteLine($"Worker {worker.Key} -> {worker.Value} word(s)/sec");
                workerStats[worker.Key] = 0;
            }
        }
    }

    static string IncrementWord(string word)
    {
        int number = int.Parse(word);
        number++;
        return number.ToString("D8");
    }
}
