using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace Client
{
    internal class Client
    {
        static void Main(string[] args)
        {
            //3. Zadatak: DER generator 
            Console.WriteLine("Izaberite tip generatora:");
            Console.WriteLine("1. Solarni panel");
            Console.WriteLine("2. Vjetrogenerator");

            int izbor = int.Parse(s: Console.ReadLine());
            double nominalnaSnaga = 0;
            string generatorType = "";

            if (izbor == 1)
            {
                Console.WriteLine("Unesite nominalnu snagu za solarni panel (100-500 kW):");
                nominalnaSnaga = UnesiNominalnuSnagu(100, 500);
                generatorType = "Solarni panel";
            }
            else if (izbor == 2)
            {
                Console.WriteLine("Unesite nominalnu snagu za vjetrogenerator (500-1000 kW):");
                nominalnaSnaga = UnesiNominalnuSnagu(500, 1000);
                generatorType = "Vjetrogenerator";
            }
            else
            {
                Console.WriteLine("Nevažeći izbor.");
                return;
            }

            // TCP konekcija sa dispečerskim serverom
            Console.WriteLine("Kreiram TCP konekciju sa dispečerskim serverom...");
            Socket tcpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 52000);

            try
            {
                tcpServerSocket.Connect(serverEndPoint);
                Console.WriteLine("Povezivanje sa serverom je uspješno.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom povezivanja sa serverom: {ex.Message}");
                return;
            }

            // UDP utičnica za slanje upravljačkih podataka
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, 60001);
            Console.WriteLine($"UDP utičnica (upravljačka) otvorena na IP adresi {udpEndPoint.Address} i portu {udpEndPoint.Port}");

            // TCP utičnica za vezu sa vremenskim senzorom
            Socket sensorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint sensorEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60002);
            Console.WriteLine($"TCP utičnica (senzorska) otvorena na IP adresi {sensorEndPoint.Address} i portu {sensorEndPoint.Port}");

            Console.WriteLine("Povezivanje sa senzorom...");
            try
            {
                sensorSocket.Connect(sensorEndPoint);
                byte[] dataToSend = Encoding.ASCII.GetBytes(generatorType);
                sensorSocket.Send(dataToSend);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri povezivanju sa senzorom: {ex.Message}");
                return;
            }

            // Primanje podataka od senzora
            byte[] buffer = new byte[1024];
            int receivedBytes = sensorSocket.Receive(buffer);
            string sensorData = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"Primljeni podaci od senzora: {sensorData}");

            if (generatorType == "Solarni panel")
            {
                var parsedData = ParseSensorData(sensorData);
                if (parsedData == null) return;

                double INS = parsedData.Value.INS;
                double Tcell = parsedData.Value.Tcell;
                Console.WriteLine($"Osunčanost (INS): {INS}, Temperatura ćelije (Tcell): {Tcell}°C");

                // Izračunavanje aktivne snage
                double activePower = IzracunajAktivnuSnaguSolarniPanel(nominalnaSnaga, INS, Tcell);
                Console.WriteLine($"Izračunata aktivna snaga za solarni panel: {activePower:F2} kW");
                Console.WriteLine("Reaktivna snaga za solarni panel je 0 kW.");
                PosaljitePodatkeNaServer(tcpServerSocket, generatorType, nominalnaSnaga, activePower, 0);
            }
            else
            {
                Console.WriteLine("Proizvodnja za vjetrogenerator se računa na drugi način.");
            }
            Console.WriteLine("Klijent završava sa radom...");
            sensorSocket.Close();
            tcpServerSocket.Close();
            Console.ReadLine();
        }

        static double UnesiNominalnuSnagu(double min, double max)
        {
            double snaga;
            while (true)
            {
                if (double.TryParse(Console.ReadLine(), out snaga) && snaga >= min && snaga <= max)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Unesite validnu nominalnu snagu između {min} i {max} kW.");
                }
            }
            return snaga;
        }

        static (double INS, double Tcell)? ParseSensorData(string data)
        {
            try
            {
                string[] sensorValues = data.Split(',');

                if (sensorValues.Length != 2) return null;

                double INS = double.Parse(sensorValues[0].Trim());
                double Tcell = double.Parse(sensorValues[1].Trim());

                return (INS, Tcell);
            }
            catch
            {
                return null;
            }
        }

        //5. Zadatak: Solarni panel
        static double IzracunajAktivnuSnaguSolarniPanel(double nominalnaSnaga, double INS, double Tcell)
        {
            double aktivnaSnaga = nominalnaSnaga * INS * 0.00095 * (1 - 0.005 * (Tcell - 25));
            return aktivnaSnaga;
        }
        //6. Zadatak: Prikaz proizvodnje
        static void PosaljitePodatkeNaServer(Socket tcpSocket, string generatorType, double nominalnaSnaga, double aktivnaSnaga, double reaktivnaSnaga)
        {
            try
            {
                string message = $"Generator: {generatorType}, Nominalna snaga: {nominalnaSnaga} kW, Aktivna snaga: {aktivnaSnaga:F2} kW, Reaktivna snaga: {reaktivnaSnaga:F2} kW";
                byte[] data = Encoding.ASCII.GetBytes(message);

                tcpSocket.Send(data);
                Console.WriteLine($"Poslato serveru: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom slanja podataka serveru: {ex.Message}");
            }
        }
    }
}
