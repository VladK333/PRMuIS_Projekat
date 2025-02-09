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
            Socket sensorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint sensorEndPoint = new IPEndPoint(IPAddress.Any, 60002);
            sensorSocket.Bind(sensorEndPoint);
            sensorSocket.Listen(5);

            Console.WriteLine("Senzor ceka na povezivanje klijenta...\n");

            //7. Zadatak: Istovremeni, neblokiraju rad sa vise klijenata
            while (true)
            {
                try
                {
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
                    else if (generatorType == "Vetrogenerator")
                    {
                        sensorData = IzracunajBrzinuVetra().ToString("F2");
                    }

                    byte[] data = Encoding.ASCII.GetBytes(sensorData);
                    clientSocket.Send(data);
                    Console.WriteLine($"Podaci poslati klijentu: {sensorData}\n");

                    clientSocket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska: {ex.Message}");
                }
            }
        }

        //4. Zadatak: Senzor vremenskih prilika (Solarni panel)
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

        //8. Zadatak: Senzor vremenskih prilika(Vetrogenerator)
        static double IzracunajBrzinuVetra()
        {
            Random rand = new Random();
            return rand.NextDouble() * 30.0;
        }
    }
}
