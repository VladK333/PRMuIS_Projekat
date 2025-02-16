using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;

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
            sensorSocket.Blocking = false;  

            Console.WriteLine("Senzor čeka na povezivanje klijenata...\n");

            //7.Zadatak: Istovremeni, neblokirajuci rad sa više klijenata
            List<Socket> clients = new List<Socket>();
            while (true)
            {
                List<Socket> cekanjeZaCitanje = new List<Socket>(clients) { sensorSocket };
                Socket.Select(cekanjeZaCitanje, null, null, 1000000);

                List<Socket> klijentiZaBrisanje = new List<Socket>();

                foreach (Socket socket in cekanjeZaCitanje)
                {
                    if (socket == sensorSocket)
                    {
                        // Novi klijent se povezao
                        try
                        {
                            Socket clientSocket = sensorSocket.Accept();
                            clientSocket.Blocking = false; 
                            clients.Add(clientSocket);
                            Console.WriteLine("Klijent povezan.");
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Greška pri prihvatanju klijenta: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Obrada podataka od postojeceg klijenta
                        byte[] buffer = new byte[1024];
                        int receivedBytes = 0;
                        try
                        {
                            receivedBytes = socket.Receive(buffer);
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Greška u komunikaciji sa klijentom: {ex.Message}. Zatvaranje konekcije.");
                            klijentiZaBrisanje.Add(socket);
                            socket.Close();
                            continue;
                        }

                        if (receivedBytes == 0)
                        {
                            Console.WriteLine("Klijent se odjavio.\n");
                            klijentiZaBrisanje.Add(socket);
                            socket.Close();
                            continue;
                        }

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
                        try
                        {
                            //3. Senzor vremenskih prilika salje izmjerene vrijednosti
                            socket.Send(data);
                            Console.WriteLine($"Podaci poslati klijentu: {sensorData}\n");
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Greška prilikom slanja podataka: {ex.Message}");
                        }

                        klijentiZaBrisanje.Add(socket);
                        socket.Close();
                    }
                }

                foreach (Socket s in klijentiZaBrisanje)
                {
                    clients.Remove(s);
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
