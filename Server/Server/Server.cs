using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace Server
{
    internal class Server
    {
        static void Main(string[] args)
        {
            //2. Zadatak: Dispečerski server 
            Socket tcpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 52000);
                tcpServerSocket.Bind(localEndPoint);
                tcpServerSocket.Listen(1);
                Console.WriteLine("TCP server je pokrenut.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greška pri povezivanju sa portom: {ex.Message}");
                return;
            }

            Socket acceptedSocket;
            try
            {
                Console.WriteLine("Čeka se na povezivanje klijenta...");
                acceptedSocket = tcpServerSocket.Accept();
                Console.WriteLine("Povezivanje sa klijentom uspjelo.");

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greška pri prihvatanju konekcije: {ex.Message}");
                return;
            }
            //6. Zadatak: Prikaz proizvodnje
            byte[] prijemniBafer = new byte[1024];
            int brBajta = acceptedSocket.Receive(prijemniBafer);
            string poruka = Encoding.ASCII.GetString(prijemniBafer, 0, brBajta);
            Console.WriteLine($"Primljena poruka: {poruka}");
            string vreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"Podaci primljeni u: {vreme}");

            Console.WriteLine("Server završava sa radom");
            acceptedSocket.Close();
            tcpServerSocket.Close();
            Console.ReadKey();
        }
    }
}
