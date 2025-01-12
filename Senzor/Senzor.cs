using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace Senzor
{
    internal class Senzor
    {
        static void Main(string[] args)
        {
            //4. Zadatak: Senzor vremenskih prilika (Solarni panel)
            Socket sensorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint sensorEndPoint = new IPEndPoint(IPAddress.Any, 60002);
            sensorSocket.Bind(sensorEndPoint);
            sensorSocket.Listen(1);

            Console.WriteLine("Senzor čeka na povezivanje klijenta...");
            Socket clientSocket = sensorSocket.Accept();
            Console.WriteLine("Klijent povezan.");

            byte[] buffer = new byte[1024];
            int receivedBytes = clientSocket.Receive(buffer);
            string generatorType = Encoding.ASCII.GetString(buffer, 0, receivedBytes).Trim();
            Console.WriteLine($"Primljen tip generatora: {generatorType}");

            string sensorData = "";

            if (generatorType == "Solarni panel")
            {
                int trenutniSat = DateTime.Now.Hour;
                var (INS, Tcell) = IzracunajOsuncanostITemperaturu(trenutniSat);
                sensorData = $"{INS},{Tcell:F2}";
            }
            else if (generatorType == "Vjetrogenerator")
            {
                Console.WriteLine("Proizvodnja za vjetrogenerator se računa na drugi način.");
            }

            byte[] data = Encoding.ASCII.GetBytes(sensorData);
            clientSocket.Send(data);
            Console.WriteLine($"Podaci poslati klijentu: {sensorData}");

            clientSocket.Close();
            sensorSocket.Close();
            Console.WriteLine("Senzor završava sa radom...");
            Console.ReadKey();
        }

        static (double INS, double Tcell) IzracunajOsuncanostITemperaturu(int sati)
        {
            double INS, Tcell;

            if (sati == 12 || sati == 13 || sati == 14)
            {
                INS = 1050;
                Tcell = 30;
            }
            else if (sati < 12)
            {
                int razlika = 12 - sati;
                INS = 1050 - (razlika * 200);
                Tcell = 30 - (razlika * 4);
            }
            else
            {
                int razlika = sati - 14;
                INS = 1050 - (razlika * 200);
                Tcell = 30 - (razlika * 4);
            }

            Tcell = Tcell > 25 ? 25 : Tcell + (0.025 * INS);
            return (INS, Tcell);
        }
    }
}
