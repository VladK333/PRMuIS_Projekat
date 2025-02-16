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
            Console.WriteLine("2. Vetrogenerator");

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
                Console.WriteLine("Unesite nominalnu snagu za vetrogenerator (500-1000 kW):");
                nominalnaSnaga = UnesiNominalnuSnagu(500, 1000);
                generatorType = "Vetrogenerator";
            }
            else
            {
                Console.WriteLine("Nevazeci izbor.");
                return;
            }

            // TCP konekcija sa dispecerskim serverom (potrebno i za 2.zadatak)
            Console.WriteLine("\nKreiram TCP konekciju sa dispecerskim serverom...");
            Socket tcpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 52000);

            try
            {
                tcpServerSocket.Connect(serverEndPoint);
                Console.WriteLine("Povezivanje sa serverom je uspesno.");
                tcpServerSocket.Blocking = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska prilikom povezivanja sa serverom: {ex.Message}");
                return;
            }


            // UDP uticnica za prijem podataka
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, 60001);
            udpSocket.Close();

            // TCP uticnica za vezu sa vremenskim senzorom
            Socket sensorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint sensorEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60002); //Klijent mora da zna tacnu IP adresu servera   
            Console.WriteLine($"TCP uticnica (senzorska) otvorena na IP adresi {sensorEndPoint.Address} i portu {sensorEndPoint.Port}");

            Console.WriteLine("Povezivanje sa senzorom...");
            try
            {
                sensorSocket.Connect(sensorEndPoint);
                byte[] dataToSend = Encoding.ASCII.GetBytes(generatorType);
                sensorSocket.Send(dataToSend);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri povezivanju sa senzorom: {ex.Message}");
                return;
            }

            // Primanje podataka od senzora
            byte[] buffer = new byte[1024];
            int receivedBytes = sensorSocket.Receive(buffer);
            string sensorData = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"Primljeni podaci od senzora: {sensorData}");

            if (generatorType == "Solarni panel")
            {
                var parsedData = ParseSensorDataSP(sensorData);
                if (parsedData == null) return;

                double INS = parsedData.Value.INS;
                double Tcell = parsedData.Value.Tcell;
                Console.WriteLine($"Osuncanost (INS): {INS}, Temperatura celije (Tcell): {Tcell}°C");

                // Izračunavanje aktivne snage
                double activePower = IzracunajAktivnuSnaguSolarniPanel(nominalnaSnaga, INS, Tcell);
                Console.WriteLine($"Izracunata aktivna snaga za solarni panel: {activePower:F2} kW");
                Console.WriteLine("Reaktivna snaga za solarni panel: 0 kVAR.");
                PosaljitePodatkeNaServer(tcpServerSocket, generatorType, activePower, 0);
            }
            else
            {
                var parsedData = ParseSensorDataV(sensorData);
                if (parsedData == null) return;

                double brzinaVetra = parsedData.Value;
                Console.WriteLine($"Brzina vetra: {brzinaVetra}");

                var (aktivnaSnaga, reaktivnaSnaga) = IzracunajSnaguVjetrogenerator(brzinaVetra, nominalnaSnaga);
                Console.WriteLine($"Izracunata aktivna snaga za vetrogenerator: {aktivnaSnaga:F2} kW.");
                Console.WriteLine($"Izracunata reaktivna snaga za vetrogenerator: {reaktivnaSnaga:F2} kVAR.");
                PosaljitePodatkeNaServer(tcpServerSocket, generatorType, aktivnaSnaga, reaktivnaSnaga);
            }
            //3.zatvaranje konekcije
            Console.WriteLine("Klijent zavrsava sa radom...");
            sensorSocket.Close();
            tcpServerSocket.Close();
            Console.ReadLine();
        }
        //3.
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
                    Console.WriteLine($"Unesite validnu nominalnu snagu izmedju {min} i {max} kW.");
                }
            }
            return snaga;
        }

        static (double INS, double Tcell)? ParseSensorDataSP(string data)
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

        static double? ParseSensorDataV(string data)
        {
            try
            {
                double brzinaVetra = double.Parse(data.Trim());
                return brzinaVetra;
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
        static void PosaljitePodatkeNaServer(Socket tcpSocket, string generatorType, double aktivnaSnaga, double reaktivnaSnaga)
        {
            try
            {
                //format izmenjen zbog 10. zadatka
                string message = $"{generatorType}|{aktivnaSnaga:F2}|{reaktivnaSnaga:F2}";
                byte[] data = Encoding.ASCII.GetBytes(message);

                tcpSocket.Send(data);
                Console.WriteLine($"Poslato serveru: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska prilikom slanja podataka serveru: {ex.Message}");
            }
        }

        //9. Zadatak: Vetrogenerator
        static (double aktivnaSnaga, double reaktivnaSnaga) IzracunajSnaguVjetrogenerator(double brzinaVetra, double nominalnaSnaga)
        {
            double aktivnaSnaga = 0;

            if (brzinaVetra < 3.5 || brzinaVetra > 25)
            {
                aktivnaSnaga = 0;
            }
            else if (brzinaVetra >= 3.5 && brzinaVetra < 14)
            {
                aktivnaSnaga = (brzinaVetra - 3.5) * 0.035;
            }
            else if (brzinaVetra >= 14 && brzinaVetra <= 25)
            {
                aktivnaSnaga = nominalnaSnaga;
            }

            double reaktivnaSnaga = aktivnaSnaga * 0.05;
            return (aktivnaSnaga, reaktivnaSnaga);
        }

       
    }
}
