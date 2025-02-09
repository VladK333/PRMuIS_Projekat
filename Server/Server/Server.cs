using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    internal class Server
    {
        static List<Proizvodnja> proizvodnja = new List<Proizvodnja>(); // Lista za čuvanje podataka o proizvodnji

        class Proizvodnja
        {
            public string IdGeneratora { get; set; } 
            public double AktivnaSnaga { get; set; }
            public double ReaktivnaSnaga { get; set; }

            public Proizvodnja(string IdGen, double aktivnaSnaga, double reaktivnaSnaga)
            {
                string randomPart = new Random().Next(1000, 9999).ToString();

                IdGeneratora =  IdGen+randomPart;
                AktivnaSnaga = aktivnaSnaga;
                ReaktivnaSnaga = reaktivnaSnaga;
            }
        }

        static void Main(string[] args)
        {
            //2. Zadatak: Dispecerski server 
            Socket tcpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            List<Socket> clients = new List<Socket>();

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Statistika);

            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 52000);
                tcpServerSocket.Bind(localEndPoint);
                tcpServerSocket.Listen(1);
                Console.WriteLine("TCP server je pokrenut.\n");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greska pri povezivanju sa portom: {ex.Message}");
                return;
            }

            //7. Zadatak: Istovremeni, neblokirajuci rad sa vise klijenata
            while (true)
            {
                List<Socket> cekanjeZaCitanje = new List<Socket>(clients) { tcpServerSocket };
                Socket.Select(cekanjeZaCitanje, null, null, -1);

                foreach (Socket socket in cekanjeZaCitanje)
                {
                    if (socket == tcpServerSocket)
                    {
                        Socket noviKlijent = tcpServerSocket.Accept();
                        clients.Add(noviKlijent);
                        Console.WriteLine("Novi klijent povezan.");
                    }
                    else
                    {
                        byte[] prijemniBafer = new byte[1024];
                        int brBajta;
                        try
                        {
                            brBajta = socket.Receive(prijemniBafer);
                            if (brBajta == 0)
                            {
                                Console.WriteLine("Klijent se odjavio.\n");
                                clients.Remove(socket);
                                socket.Close();
                                continue;
                            }
                            //6. Zadatak: Prikaz proizvodnje
                            string poruka = Encoding.ASCII.GetString(prijemniBafer, 0, brBajta);
                            ObradiPrimljenePodatke(poruka);
                            string vreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            Console.WriteLine($"Podaci primljeni u: {vreme}");
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine("Greska u komunikaciji sa klijentom. Zatvaranje konekcije.");
                            clients.Remove(socket);
                            socket.Close();
                        }
                    }
                }
            }
        }

        static void ObradiPrimljenePodatke(string primljeniPodaci)
        {
            try 
            {
                string[] delovi = primljeniPodaci.Split('|');

                if(delovi.Length == 3)
                {
                    string generatorType = delovi[0];
                    double aktivnaSnaga = double.Parse(delovi[1]);
                    double reaktivnaSnaga = double.Parse(delovi[2]);

                    string idGen = generatorType == "Solarni panel" ? "sp" : "vg";

                    Console.WriteLine($"Primljena poruka:\nGenerator: {generatorType}, Aktivna snaga: {aktivnaSnaga} kW, Reaktivna snaga: {reaktivnaSnaga} kVAR");

                    //10. Zadatak: Sakupljanje informacija o proizvodnji
                    Proizvodnja novaProizvodnja = new Proizvodnja(idGen, aktivnaSnaga, reaktivnaSnaga);
                    proizvodnja.Add(novaProizvodnja);
                    Console.WriteLine($"Novi generator dodat u listu: ID = {novaProizvodnja.IdGeneratora}");
                }
                else
                {
                    Console.WriteLine("Greska: Neispravan format podataka.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska prilikom obrade podataka: {ex.Message}");
            }
        } 
        
        //11. Zadatak: Statistika
        static void Statistika(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            double ukupnaSolarna = proizvodnja.Where(p => p.IdGeneratora.StartsWith("sp")).Sum(p => p.AktivnaSnaga);
            double ukupnaVetrogenerator = proizvodnja.Where(p => p.IdGeneratora.StartsWith("vg")).Sum(p => p.AktivnaSnaga);

            Console.WriteLine("\n===STATISTIKA===");
            Console.WriteLine($"Ukupna proizvodnja aktivne snage solarnih panela iznosi: {ukupnaSolarna} kW");
            Console.WriteLine($"Ukupna proizvodnja aktivne snage vetrogeneratora iznosi: {ukupnaVetrogenerator} kW");

            if (ukupnaSolarna > ukupnaVetrogenerator)
                Console.WriteLine("Solarni paneli su proizveli vise energije.");
            else if (ukupnaSolarna < ukupnaVetrogenerator)
                Console.WriteLine("Vetrogeneratori su proizveli vise energije.");
            else
                Console.WriteLine("Solarni paneli i vetrogeneratori su proizveli istu kolicinu energije.");

            Console.WriteLine("Zatvaranje servera...");
            Environment.Exit(0 );


        }
    }
}
