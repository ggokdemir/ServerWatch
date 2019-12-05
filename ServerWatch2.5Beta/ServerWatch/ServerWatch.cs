using ServerWatch;
using System;
using System.Threading;
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
                SetTextMain(" MAIN:[" + ServerLoads.mainAmount.ToString() + "]  SUB1:[" + ServerLoads.sAmount[0] + "]  SUB2:[" + ServerLoads.sAmount[1]
                    + "]  SUB3:[" + ServerLoads.sAmount[2] + "]  SUB4:[" + ServerLoads.sAmount[3] + "]  SUB5:[" + ServerLoads.sAmount[4] + "]  SUB6:[" + ServerLoads.sAmount[5]
    + "]  SUB7:[" + ServerLoads.sAmount[6] + "]  SUB8:[" + ServerLoads.sAmount[7] + "]  SUB9:[" + ServerLoads.sAmount[8] + "]  SUB10:[" + ServerLoads.sAmount[9] + "]");
            }
        }

        public void Servers()
        {
            RequestTaker requestTaker = new RequestTaker(0, 0); //server1 takes 0 load from main server (we called this for to start thread)
            MainServer mainServer = new MainServer(); //starts main server constructor
        }

        public void Checker()
        {
            SubServerChecker serverChecker = new SubServerChecker();
            serverChecker.SubServerCheckerMethod();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            Thread visualize = new Thread(new ThreadStart(Visualizer)); //text thread
            visualize.Start();

            Thread servers = new Thread(new ThreadStart(Servers)); //main thread
            servers.Start();

            Thread checker = new Thread(new ThreadStart(Checker)); //control thread
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
        double capacity = 500; //I set to 500 instead of 1000

        public SubServer(int _serverNo)
        {
            serverNo = _serverNo;
            Console.WriteLine("SubServer " + serverNo + " started.");
            ServerLoads.sAmount[serverNo - 1] = -1;

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
                        ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] - number1; //server random amount of load
                    }

                    if (ServerLoads.sAmount[serverNo - 1] < 0)
                    {
                        ServerLoads.sAmount[serverNo - 1] = 0; //servers cannot take negative load
                    }
                }

                Thread.Sleep(200);  //sleep 300+200=500ms

                //Take random(1-50) number in every 500ms from mainServer.
                RandomGenerator rand2 = new RandomGenerator();
                if (ServerLoads.mainAmount > 0)
                {
                    RequestTaker requestTaker = new RequestTaker(rand2.number, serverNo);   //takes randoom load from main server
                    ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] + rand2.number; //then adds that random load to subserver
                }
            }
        }

        public SubServer(int _serverNo, int _otherServerNo) //we use this constructor for taking load from another subserver
        {
            serverNo = _serverNo;

            ServerLoads.sAmount[serverNo - 1] = Math.Ceiling(ServerLoads.sAmount[_otherServerNo - 1] / 2); //half of the load goes to this subserver
            Console.WriteLine("SubServer " + serverNo + " started with " + ServerLoads.sAmount[serverNo - 1]);
            ServerLoads.sAmount[_otherServerNo - 1] = Math.Floor(ServerLoads.sAmount[_otherServerNo - 1] / 2); //another half stays on other subserver
            Console.WriteLine("SubServer " + _otherServerNo + " = " + ServerLoads.sAmount[_otherServerNo - 1]);

            while (true) //subserver takes random load periodicaly from main server and responds them randomly
            {
                Thread.Sleep(300);  //sleep 300ms
                //Generate random(1-50) number in every 500ms.
                RandomGenerator rand1 = new RandomGenerator();
                if (ServerLoads.sAmount[serverNo - 1] < capacity)
                {
                    if (ServerLoads.sAmount[serverNo - 1] > 0)
                    {
                        double number1 = rand1.number;
                        ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] - number1; //server responds random load periodically
                    }

                    if (ServerLoads.sAmount[serverNo - 1] < 0)
                    {
                        ServerLoads.sAmount[serverNo - 1] = 0; //server load cannot be negative
                    }
                }

                Thread.Sleep(200);  //sleep 300+200=500ms

                //Take random(1-50) number in every 500ms from mainServer.
                RandomGenerator rand2 = new RandomGenerator();
                if (ServerLoads.mainAmount > 0)
                {
                    RequestTaker requestTaker = new RequestTaker(rand2.number, serverNo); //takes random load from mainserver
                    ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] + rand2.number; //then responds them randomly
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
    public MainServer() //Main Thread
    {
        Console.WriteLine("MainServer started.");
        Thread.CurrentThread.Name = "mainServer";

        ServerCreater serverCreater = new ServerCreater();
        serverCreater.First2ServerCreater(); //at start we run two subserver thread 
        Thread.Sleep(100);
        while (true)
        {
            Thread.Sleep(100); //100+100=200ms
            RandomGenerator rand1 = new RandomGenerator();
            ServerLoads.mainAmount = ServerLoads.mainAmount - rand1.number; //responds randomly
            if (ServerLoads.mainAmount < 0)
            {
                ServerLoads.mainAmount = 0; //server load cannot be negative
            }

            Thread.Sleep(200);
            RandomGenerator rand2 = new RandomGenerator();
            ServerLoads.mainAmount = ServerLoads.mainAmount - rand2.number; //responds randomly
            if (ServerLoads.mainAmount < 0)
            {
                ServerLoads.mainAmount = 0; //server load cannot be negative
            }

            Thread.Sleep(100); //200+200+100=500ms
            //Generate random(1-100) number in every 500ms
            RandomGenerator100 rand100 = new RandomGenerator100();
            ServerLoads.mainAmount = ServerLoads.mainAmount + rand100.number;
            if (ServerLoads.mainAmount > capacity)
            {
                ServerLoads.mainAmount = capacity; //server cannot take more load than its capacity
            }

            Thread.Sleep(100); //100+100=200ms
            RandomGenerator rand3 = new RandomGenerator();
            ServerLoads.mainAmount = ServerLoads.mainAmount - rand3.number; //responds randomly
            if (ServerLoads.mainAmount < 0)
            {
                ServerLoads.mainAmount = 0; //server load cannot be negative
            }
        }
    }
}

class ServerThread //subserver thread list
{
    public static Thread[] threads = new Thread[10]; //Max 10 subserver available 
}
class ServerCreater //class that creates subservers
{
    public ServerCreater()
    {
        //empty
    }

    public void First2ServerCreater() //creates first two subservers at start
    {
        // Create threads amount of subServerNeeded
        for (int i = 0; i < SubServerCount.subServerNeeded; i++)
        {
            Thread t = new Thread(new ThreadStart(SubServerCreater));
            t.Name = "subServer" + (i + 1).ToString();
            ServerThread.threads[i] = t;
        }

        // Start threads
        for (int i = 0; i < SubServerCount.subServerNeeded; i++)
        {
            Thread.Sleep(100); //to avoid conflict
            ServerThread.threads[i].Start();
            SubServerCount.subServerCount++;
        }
    }

    public void SubServerCreater() //thread only takes void methods
    {
        SubServerCreater subServerCreater = new SubServerCreater();
    }
}

class SubServerCreater //we need two constructor 
{
    public SubServerCreater()
    {
        SubServer subServer = new SubServer(SubServerCount.subServerCount);
    }

    public SubServerCreater(int otherServerNo)
    {
        SubServer subServer = new SubServer(SubServerCount.subServerCount, otherServerNo);
    }
}

class SubServerRemover
{
    public SubServerRemover(int i)
    {
        Thread.Sleep(100);
        //Method that ends threads
    }
}

class SubServerChecker
{
    int lastAmount = -1; //we use this for cheking that are we creating new server because of the same otherserver
    int otherServerNo = -1; //the server that reaches critical 
    public SubServerChecker()
    {
        SubServerCheckerMethod();
    }

    public void SubServerCheckerMethod()
    {
        while (true)
        {
            Thread.Sleep(200); //Should sleep for a 200ms (changes happens in min 200ms)
            for (int i = 0; i < 10; i++)
            {
                if (ServerLoads.sAmount[i] > 70) //Capacity is 500. So it is %70
                {
                    if (lastAmount != (int)ServerLoads.sAmount[i]) //if something changes check again
                    {
                        if (SubServerCount.subServerCount < 10) //we have 10 servers
                        {
                            Console.WriteLine("SUBSERVER CREATED: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                            SubServerCount.subServerCount++;
                            otherServerNo = (i + 1);
                            Thread subServerCreaterThread = new Thread(new ThreadStart(CreateSubServerHere));
                            subServerCreaterThread.Start();
                        }
                        else
                        {
                            Console.WriteLine("OUT OF SERVERS: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                        }
                    }
                    lastAmount = (int)ServerLoads.sAmount[i];
                }
                if (ServerLoads.sAmount[i] == 0) //if load is zero we remove this server
                {
                    if (lastAmount != (int)ServerLoads.sAmount[i])
                    {
                        if (SubServerCount.subServerCount > 2) //2 subservers must stay
                        {
                            RemoveSubServerHere(i); //
                            SubServerCount.subServerCount--;
                        }
                    }
                    lastAmount = (int)ServerLoads.sAmount[i]; //to check changes
                }
            }
        }
    }

    public void CreateSubServerHere() //threads can take void methods
    {
        SubServerCreater subServerCreater = new SubServerCreater(otherServerNo);
    }

    public void RemoveSubServerHere(int i)
    {
        SubServerRemover subServerRemover = new SubServerRemover(i);
    }
}

class ServerLoads //Stores load of servers
{
    public static double mainAmount = 0; //MainServer load
    public static double[] sAmount = new double[10]; //SubServers' loads
}

class SubServerCount //server count information
{
    //min 2 subserver required
    public static int subServerNeeded = 2;
    public static int subServerCount = 0;
}

class RequestTaker
{
    private Object loadLock = new Object(); //locks for if two threads trys to use same time

    public RequestTaker(double amt, int no)
    {
        lock (loadLock)
        {
            if ((ServerLoads.mainAmount - amt) < 0) //subserver takes all load
            {
                ServerLoads.sAmount[no] = ServerLoads.sAmount[no] + ServerLoads.mainAmount;
                ServerLoads.mainAmount = 0;
            }

            if (ServerLoads.mainAmount >= amt)
            {
                Console.WriteLine("Removed {0} and {1} left in MainServer by Server {2}", amt, (ServerLoads.mainAmount - amt), no);
                ServerLoads.mainAmount -= amt;

            }
        }
    }
}