using ServerWatch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ServerWatch
{
    public partial class ServerWatch : Form
    {

        public ServerWatch()
        {
            InitializeComponent();
        }

        delegate void SetTextCallback(string text);

        private void SetTextMain(string text)
        {
            if (this.textBoxBanner.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTextMain);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBoxBanner.Text = text;
            }
        }


        public void Visualizer()
        {
            while (true)
            {
                SetTextMain("MAIN:" + ServerLoads.mainAmount.ToString() + " SUB1:" + ServerLoads.sAmount[0] + " SUB2:" + ServerLoads.sAmount[1]
                    + " SUB3:" + ServerLoads.sAmount[2] + " SUB4:" + ServerLoads.sAmount[3] + " SUB5:" + ServerLoads.sAmount[4]);
            }
        }

        public void Servers()
        {
            RequestTaker requestTaker = new RequestTaker(0, 0);
            MainServer mainServer = new MainServer(); //starts main server constructor
        }

        public void Checker()
        {
            SubServerChecker serverChecker = new SubServerChecker();
            serverChecker.SubServerCheckerMethod();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {

            Thread visualize = new Thread(new ThreadStart(Visualizer));
            visualize.Start();

            Thread servers = new Thread(new ThreadStart(Servers));
            servers.Start();

            Thread checker = new Thread(new ThreadStart(Checker));
            checker.Start();

        }

        private void ServerWatch_Load(object sender, EventArgs e)
        {
            //EMPTY
        }
    }



    class SubServer
    {
        int serverNo { get; set; }
        double amount { get; set; }
        double capacity = 1000;
        string Name { get; set; }

        public SubServer(int _serverNo)
        {
            serverNo = _serverNo;
            Console.WriteLine("SubServer " + serverNo + " started.");
            ServerLoads.sAmount[serverNo - 1] = 0;

            while (true)
            {
                Thread.Sleep(300);  //sleep 300ms
                //Generate random(1-50) number in every 500ms.
                RandomGenerator rand1 = new RandomGenerator();
                if (ServerLoads.sAmount[serverNo - 1] < capacity)
                {
                    if (ServerLoads.sAmount[serverNo - 1] > 0)
                    {
                        double number1 = rand1.number;
                        ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] - number1;
                    }

                    if (ServerLoads.sAmount[serverNo - 1] < 0)
                    {
                        ServerLoads.sAmount[serverNo - 1] = 0;
                    }
                }


                Thread.Sleep(200);  //sleep 300+200=500ms

                //Take random(1-50) number in every 500ms from mainServer.
                RandomGenerator rand2 = new RandomGenerator();
                if (ServerLoads.mainAmount > 0)
                {
                    RequestTaker requestTaker = new RequestTaker(rand2.number, serverNo);
                    ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] + rand2.number;
                }

            }
        }
    }
}

public class RandomGenerator //Generates number between 0 and 50
{
    private static readonly Random random = new Random();

    public double number;

    public RandomGenerator()
    {
        number = random.Next(50);
    }
}

public class RandomGenerator100 //Generates number between 0-100
{
    private static readonly Random random = new Random();

    public double number;

    public RandomGenerator100()
    {
        number = random.Next(100);
    }
}


class MainServer
{
    double capacity = 1000;
    string Name { get; set; }
    public MainServer() //Main Thread
    {
        Console.WriteLine("MainServer started.");
        Thread.CurrentThread.Name = "mainServer";

        ServerCreater serverCreater = new ServerCreater();
        serverCreater.First2ServerCreater();
        Thread.Sleep(100);
        while (true)
        {
            Thread.Sleep(100); //100+100=200ms
            RandomGenerator rand1 = new RandomGenerator();
            ServerLoads.mainAmount = ServerLoads.mainAmount - rand1.number;
            if (ServerLoads.mainAmount < 0)
            {
                ServerLoads.mainAmount = 0;
            }

            Thread.Sleep(200);
            RandomGenerator rand2 = new RandomGenerator();
            ServerLoads.mainAmount = ServerLoads.mainAmount - rand2.number;
            if (ServerLoads.mainAmount < 0)
            {
                ServerLoads.mainAmount = 0;
            }

            Thread.Sleep(100); //200+200+100=500ms
            //Generate random(1-100) number in every 500ms
            RandomGenerator100 rand100 = new RandomGenerator100();
            ServerLoads.mainAmount = ServerLoads.mainAmount + rand100.number;
            if (ServerLoads.mainAmount > capacity)
            {
                ServerLoads.mainAmount = capacity;
            }

            Thread.Sleep(100); //100+100=200ms
            RandomGenerator rand3 = new RandomGenerator();
            ServerLoads.mainAmount = ServerLoads.mainAmount - rand3.number;
            if (ServerLoads.mainAmount < 0)
            {
                ServerLoads.mainAmount = 0;
            }
        }
    }
}


class ServerCreater
{

    Thread[] threads = new Thread[10]; //Max 10 subserver available 
    public ServerCreater()
    {
        //empty
    }

    public void First2ServerCreater()
    {
        // Create threads amount of subServerNeeded
        for (int i = 0; i < SubServerCount.subServerNeeded; i++)
        {
            Thread t = new Thread(new ThreadStart(SubServerCreater));
            t.Name = "subServer" + (i + 1).ToString();
            threads[i] = t;
        }

        // Start threads
        for (int i = 0; i < SubServerCount.subServerNeeded; i++)
        {
            Thread.Sleep(100); //to 
            threads[i].Start();
            SubServerCount.subServerCount++;
        }
    }

    public void SubServerCreater()
    {
        SubServerCreater subServerCreater = new SubServerCreater();
    }
}


class SubServerCreater
{
    public SubServerCreater()
    {
        SubServer subServer = new SubServer(SubServerCount.subServerCount);
    }
}

class SubServerChecker
{
    int lastAmount = -1;
    public SubServerChecker()
    {
        SubServerCheckerMethod();
    }

    public void SubServerCheckerMethod()
    {
        while (true)
        {
            Console.WriteLine("LOOP");
            Thread.Sleep(200); //Should sleep for a 200ms
            for (int i = 0; i < 10; i++)
            {
                if (ServerLoads.sAmount[i] > 70) //Should be %70 but doesn't fit it
                {
                    if (lastAmount != (int)ServerLoads.sAmount[i])
                    {
                        if (SubServerCount.subServerCount < 10)
                        {
                            Console.WriteLine("SUBSERVER CREATED: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                            SubServerCount.subServerCount++;
                            Thread subServerCreaterThread = new Thread(new ThreadStart(CreateSubServerHere));
                            subServerCreaterThread.Start();

                        }
                        else
                        {
                            Console.WriteLine("OUT OF SERVERS: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                        }
                    }
                    Console.WriteLine("SubServerCount : " + SubServerCount.subServerCount);
                    lastAmount = (int)ServerLoads.sAmount[i];
                }
                if (ServerLoads.sAmount[i] < 0)
                {
                    Console.WriteLine("SUBSERVER REMOVED: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                    if (lastAmount != (int)ServerLoads.sAmount[i])
                    {
                        if (SubServerCount.subServerCount > 2)
                        {
                            SubServerCount.subServerCount--;
                        }
                    }
                    Console.WriteLine("SubServerCount : " + SubServerCount.subServerCount);
                    lastAmount = (int)ServerLoads.sAmount[i];
                    //SubServerRemover(i);
                }
            }
        }
    }
    public void CreateSubServerHere()
    {
        SubServerCreater subServerCreater = new SubServerCreater();
    }
}


class ServerLoads //Stores load of servers
{
    public static double mainAmount = 0; //MainServer load
    public static double[] sAmount = new double[10]; //SubServers' loads
}

class SubServerCount
{
    //min 2 subserver required
    public static int subServerNeeded = 2;
    public static int subServerCount = 0;
    public static int newServerCount = 0;
}


class RequestTaker
{

    private Object loadLock = new Object();

    public RequestTaker(double amt, int no)
    {

        if ((ServerLoads.mainAmount - amt) < 0)
        {
            //Console.WriteLine($" {ServerLoads.mainAmount} left in Server");
        }

        lock (loadLock)
        {
            if (ServerLoads.mainAmount >= amt)
            {
                Console.WriteLine("Removed {0} and {1} left in MainServer by Server {2}", amt, (ServerLoads.mainAmount - amt), no);
                ServerLoads.mainAmount -= amt;

            }
        }
    }
}