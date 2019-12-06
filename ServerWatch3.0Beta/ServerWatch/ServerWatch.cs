using ServerWatch;
using System;
using System.Linq;
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
                Thread.Sleep(100);
                SetTextMain(" MAIN:[" + ServerLoads.mainAmount.ToString() + "]  SUB1:[" + ServerLoads.sAmount[0] + "]  SUB2:[" + ServerLoads.sAmount[1]
                    + "]  SUB3:[" + ServerLoads.sAmount[2] + "]  SUB4:[" + ServerLoads.sAmount[3] + "]  SUB5:[" + ServerLoads.sAmount[4] + "] ");
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
            for (int i = 0; i < SubServerIsRunning.isRunning.Count(); i++)
            {
                SubServerIsRunning.isRunning[i] = false; //By default any server is not running.
            }

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
    class RequestTaker
    {
        private Object loadLock = new Object(); //locks for if two threads trys to use same time

        public RequestTaker(double amt, int serverNo)
        {
            lock (loadLock)
            {
                if ((ServerLoads.mainAmount - amt) < 0) //subserver takes all load
                {
                    ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] + ServerLoads.mainAmount;
                    ServerLoads.mainAmount = 0;
                }

                if (ServerLoads.mainAmount >= amt)
                {
                    Console.WriteLine("Removed {0} and {1} left in MainServer by Server {2}", amt, (ServerLoads.mainAmount - amt), serverNo);
                    ServerLoads.mainAmount -= amt;

                }
            }
        }
    }
    class SubServerChecker
    {
        int lastAmount = -1; //we use this for cheking that are we creating new server because of the same otherserver
        int lastRemoved = -1; //we use this for cheking that are we trying to abort the thread that we just aborted
        int otherServerNo = -1; //the server that reaches critical 
        int[] removed = new int[6];
        public SubServerChecker()
        {
            SubServerCheckerMethod();
        }

        public void SubServerCheckerMethod()
        {
            while (true)
            {
                Thread.Sleep(200); //Should sleep for a 200ms (changes happens in min 200ms)
                for (int i = 0; i <= SubServerCount.serverThreadCount; i++)
                {
                    if (ServerLoads.sAmount[i] > 70) //Capacity is 500. So it is %70 = 350
                    {
                        if (lastAmount != (int)ServerLoads.sAmount[i]) //if something changes check again
                        {
                            if ((SubServerCount.subServerCount) < 5) //we have 5 servers
                            {
                                SubServerCount.serverThreadCount++;
                                Console.WriteLine("SUBSERVER " + (SubServerCount.subServerCount + 1) + " CREATED: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                                otherServerNo = (i + 1);
                                Thread subServerCreaterThread = new Thread(new ThreadStart(CreateSubServerHere));
                                subServerCreaterThread.Start();
                            }
                            else
                            {
                                Console.WriteLine("OUT OF THREADS: (" + (i + 1) + ".Server = " + ServerLoads.sAmount[i] + ")");
                                int j = 0;
                                bool key = true;
                                while (key)
                                {
                                    if (SubServerIsRunning.isRunning[j] == false)
                                    {
                                        SubServerCount.serverThreadCount++;
                                        SubServerIsRunning.isRunning[j] = true;
                                        Console.WriteLine("Server " + (j + 1) + " started again");
                                        j = 0;
                                        key = false;
                                    }
                                    j++;
                                    if (j > 4)
                                    {
                                        j = 0;
                                    }
                                }


                            }
                            lastAmount = (int)ServerLoads.sAmount[i];
                        }
                    }
                    else if (ServerLoads.sAmount[i] == 0) //if load is zero we remove this server
                    {
                        if (lastAmount != (int)ServerLoads.sAmount[i])
                        {
                            if (lastRemoved != i)
                            {
                                if (SubServerCount.subServerCount > 2) //2 subservers must stay
                                {
                                    if (!removed.Contains(i))
                                    {

                                        lastRemoved = i;
                                        removed[i] = i;
                                        if (SubServerCount.serverThreadCount < 3)
                                        {
                                            SubServerCount.subServerCount--;
                                        }
                                        Thread.Sleep(100);
                                        SubServerIsRunning.isRunning[(i + 1)] = false;
                                    }
                                }
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
    }
    class SubServerCreater //we need two constructor 
    {
        private Object loadLock = new Object(); //locks for if two threads trys to use same time
        public SubServerCreater()
        {
            SubServer subServer = new SubServer(SubServerCount.subServerCount);
        }

        public SubServerCreater(int otherServerNo)
        {
            lock (loadLock)
            {
                SubServer subServer = new SubServer(SubServerCount.subServerCount, otherServerNo);
            }
        }
    }
    class SubServer
    {
        int serverNo = 1;
        double capacity = 500; //I set to 500 instead of 1000

        public SubServer(int _serverNo)
        {
            SubServerCount.subServerCount++;
            SubServerMethod(++_serverNo);
        }

        public void SubServerMethod(int _serverNo)
        {
            serverNo = _serverNo;
            Console.WriteLine("SubServer " + serverNo + " started.");
            ServerLoads.sAmount[serverNo - 1] = 0;

            SubServerIsRunning.isRunning[serverNo - 1] = true; //SubServer is running.

            while (SubServerIsRunning.isRunning[serverNo - 1])
            {
                Thread.Sleep(300);  //sleep 300ms
                //Generate random(1-50) number in every 500ms.
                RandomGenerator rand1 = new RandomGenerator();
                if (ServerLoads.sAmount[serverNo - 1] < capacity)
                {
                    if (ServerLoads.sAmount[serverNo - 1] >= 0)
                    {
                        double number1 = rand1.number;
                        if (number1 >= ServerLoads.sAmount[serverNo - 1])
                        {
                            ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] - number1; //server random amount of load
                        }
                        else
                        {
                            ServerLoads.sAmount[serverNo - 1] = 0;
                        }
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
            ServerLoads.sAmount[serverNo - 1] = -1;
            SubServerCount.serverThreadCount--;
            Console.WriteLine("! " + serverNo + " STOPED.");
            bool key = true;
            while (key)
            {
                Thread.Sleep(50);
                if (SubServerIsRunning.isRunning[serverNo - 1])
                {
                    key = false;
                    SubServerMethod(serverNo);

                }
            }

        }

        public SubServer(int _serverNo, int _otherServerNo) //we use this constructor for taking load from another subserver
        {
            SubServerCount.subServerCount++;
            SubServerMethod(++_serverNo, _otherServerNo);
        }

        public void SubServerMethod(int _serverNo, int _otherServerNo)
        {
            serverNo = (_serverNo);

            ServerLoads.sAmount[serverNo - 1] = Math.Ceiling(ServerLoads.sAmount[_otherServerNo - 1] / 2); //half of the load goes to this subserver
            Console.WriteLine("SubServer " + serverNo + " started with " + ServerLoads.sAmount[serverNo - 1]);
            ServerLoads.sAmount[_otherServerNo - 1] = Math.Floor(ServerLoads.sAmount[_otherServerNo - 1] / 2); //another half stays on other subserver
            Console.WriteLine("SubServer " + _otherServerNo + " = " + ServerLoads.sAmount[_otherServerNo - 1]);

            while (SubServerIsRunning.isRunning[serverNo - 1]) //subserver takes random load periodicaly from main server and responds them randomly
            {
                Thread.Sleep(300);  //sleep 300ms
                //Generate random(1-50) number in every 500ms.
                RandomGenerator rand1 = new RandomGenerator();
                if (ServerLoads.sAmount[serverNo - 1] < capacity)
                {
                    if (ServerLoads.sAmount[serverNo - 1] > 0)
                    {
                        double number1 = rand1.number;
                        if (number1 >= ServerLoads.sAmount[serverNo - 1])
                        {
                            ServerLoads.sAmount[serverNo - 1] = ServerLoads.sAmount[serverNo - 1] - number1; //server random amount of load
                        }
                        else
                        {
                            ServerLoads.sAmount[serverNo - 1] = 0;
                        }
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

            ServerLoads.sAmount[serverNo - 1] = -1;
            Console.WriteLine("! " + serverNo + " STOPPED.");
            SubServerCount.serverThreadCount--;

            bool key = true;
            while (key)
            {
                Thread.Sleep(50);
                if (SubServerIsRunning.isRunning[serverNo - 1])
                {
                    key = false;
                    SubServerMethod(serverNo, _otherServerNo);
                }
            }
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
                ServerLoads.mainAmount = ServerLoads.mainAmount + rand100.number;  //This command creates load for main server
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
    class ServerThreads //subserver thread list
    {
        public static Thread[] threads = new Thread[5]; //Max 5 subserver available 
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
                ServerThreads.threads[i] = t;
            }

            // Start threads
            for (int i = 0; i < SubServerCount.subServerNeeded; i++)
            {
                Thread.Sleep(100); //to avoid conflict
                ServerThreads.threads[i].Start();
            }
        }

        public void SubServerCreater() //thread only takes void methods
        {
            SubServerCreater subServerCreater = new SubServerCreater();
        }
    }

    class ServerLoads //Stores load of servers
    {
        public static double mainAmount = 0; //MainServer load
        public static double[] sAmount = new double[5]; //SubServers' loads
    }
    class SubServerCount //Stores counts of servers
    {
        //min 2 subserver required
        public static int subServerNeeded = 2;
        public static int subServerCount = 0;
        public static int serverThreadCount = 0; //counts threats
    }
    class SubServerIsRunning
    {
        public static bool[] isRunning = new bool[5]; //Is SubServer running. (-1 is not running.)
    }
}