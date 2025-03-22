using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class WorkerNode
{
    static string password = "";
    static void Main()
    {
        Console.WriteLine("Enter coordinator IP Address: ");
        string ipAddress = Console.ReadLine();
        try
        {
            TcpClient client = new TcpClient(ipAddress, 5000);
            NetworkStream stream = client.GetStream();

            // İlk mesajda şifreyi al
            byte[] buffer = new byte[256];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string firstMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            if (firstMessage.StartsWith("SECRET:"))
            {
                password = firstMessage.Substring(7);
                Console.WriteLine("Password received! Starting attempts");
            }

            while (true)
            {
                buffer = new byte[256];
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string attempt = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Trying: " + attempt);

                if (BruteForceAttempt(attempt))
                {
                    Console.WriteLine("Password found: " + attempt);
                    byte[] foundMessage = Encoding.UTF8.GetBytes("FOUND:" + attempt);
                    stream.Write(foundMessage, 0, foundMessage.Length);
                    break;
                }
                else
                {
                    byte[] continueMessage = Encoding.UTF8.GetBytes("CONTINUE");
                    stream.Write(continueMessage, 0, continueMessage.Length);
                }
            }

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    static bool BruteForceAttempt(string attempt)
    {
        return attempt == password;
    }
}
