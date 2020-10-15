using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using STAN.Client;
using NATS.Client;

namespace StanClientResourceTest
{
    class Program
    {
        class ResourceTest
        {
            private static void Log(string fmt, params object[] ps)
            {
                Console.WriteLine(DateTime.Now + ": " + string.Format(fmt, ps));
            }

            private static void Log(string fmt)
            {
                Console.WriteLine(DateTime.Now + ": " + fmt);
            }

            private static IStanConnection CreateConnection(string url, string clusterID, string clientID)
            {
                var nopts = ConnectionFactory.GetDefaultOptions();
                nopts.Url = url;
                nopts.Name = clientID;

                nopts.DisconnectedEventHandler = (obj, args) =>
                {
                    Log("({0}) Disconnected from NATS.", clientID);
                };

                nopts.ReconnectedEventHandler = (obj, args) =>
                {
                    Log("({0}) Reconnected to NATS.", clientID);
                };
                nopts.AsyncErrorEventHandler = (obj, args) =>
                {
                    Log("({0}) AsyncError: {1}.", clientID, args.Error);
                };

                var sopts = StanOptions.GetDefaultOptions();
                sopts.ConnectionLostEventHandler = (obj, args) =>
                {
                    Log("({0} STAN Connection lost.  Exiting.", clientID);
                    Environment.Exit(1);
                };

                sopts.PubAckWait = 10000;
                sopts.MaxPubAcksInFlight = 32;
                sopts.NatsConn = new ConnectionFactory().CreateConnection(nopts);
                var sc = new StanConnectionFactory().CreateConnection(clusterID, clientID, sopts);

                return sc;
            }

            internal class Subscriber
            {
                // Finished Event
                AutoResetEvent FeV = new AutoResetEvent(false);

                // Metrics
                int counter = 0;
                Stopwatch sw = null;

                public Subscriber() { }

                public void ProcessMsg(object obj, StanMsgHandlerArgs args)
                {
                    if (args.Message.Redelivered)
                    {
                        Log("Received redelivered message: " + args.Message.Sequence);
                    }

                    if (sw == null)
                    {
                        sw = new Stopwatch();
                        sw.Start();
                    }

                    try
                    {
                        args.Message.Ack();
                    }
                    catch (Exception e)
                    {
                        Log("Subscriber Ack Exception: {0}", e.Message);
                    }

                    counter++;
                    if (counter % 500 == 0)
                    {
                        Log("Subscriber Received {0} msgs at {1:0} msgs/sec.", counter, counter / sw.Elapsed.TotalSeconds);
                    }
                }

                internal void ProcessMsgs(string url, string clusterID)
                {
                    IStanConnection sc = null;
                    try
                    {
                        sc = CreateConnection(url, clusterID, "synadia-rel-sub");

                        var opts = StanSubscriptionOptions.GetDefaultOptions();
                        opts.AckWait = 60000;
                        opts.ManualAcks = true;
                        opts.MaxInflight = 32;
                        opts.DurableName = "synadia-restest";

                        sc.Subscribe("synadia.restest", "qg", opts, ProcessMsg);

                        FeV.WaitOne();
                    }
                    catch (Exception e)
                    {
                        Log("Create Subscriber failed: " + e);
                    }
                    finally
                    {
                        sc?.Close();
                    }

                    Log("Subscriber is finished.");
                }

                internal void SetFinished()
                {
                    FeV.Set();
                }
            }

            internal class Publisher
            {
                public Publisher() { }

                private readonly Process Proc = Process.GetCurrentProcess();
                private readonly Stopwatch ResourceSw = Stopwatch.StartNew();
                private readonly int processorCount = Environment.ProcessorCount; 
                private TimeSpan PrevCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

                private void PrintResourceStats()
                {
                    var Elapsed = ResourceSw.Elapsed.TotalMilliseconds;

                    // print every three seconds
                    if (Elapsed < 3000)
                        return;

                    var DeltaCpuTime = (Proc.TotalProcessorTime - PrevCpuTime).TotalMilliseconds;
                    double CpuPercentage = (DeltaCpuTime / (processorCount * Elapsed)) * 100;

                    /// Nice to have if the image/system supports it.
                    try
                    {
                        Log("System Resources:");
                        Log("   Working set  : {0} bytes", Proc.WorkingSet64);
                        Log("   Paged Memory : {0} bytes", Proc.PagedMemorySize64);
                        Log("   GC Memory    : {0} bytes", GC.GetTotalMemory(false));
                        Log("   CPU Time     : {0} sec", Proc.TotalProcessorTime.TotalSeconds);
                        Log("   CPU Percent  : {0:0.0} %", CpuPercentage * 10);
                    }
                    catch { /* NOOP - not supported on OS */ }

                    PrevCpuTime = Proc.TotalProcessorTime;
                    ResourceSw.Reset();
                    ResourceSw.Start();
                }

                private static readonly byte[] payload = new byte[1024 * 15];

                internal void SendMessages(string url, string clusterID)
                {
                    IStanConnection sc = null;
                    sc = CreateConnection(url, clusterID, "synadia-rel-publisher");
                    for (int i = 0; !IsFinished(); i++)
                    {
                        try
                        {
                            sc.Publish("synadia.restest", payload, (obj, args) => { /* NOOP */ });
                            // sync publish for long running stability
                            // sc.Publish("synadia.restest", payload); ;
                        }
                        catch (Exception e)
                        {
                            Log("Publish Exception: " + e.Message);
                            Thread.Sleep(250);
                        }

                        if (i % 500 == 0)
                        {
                            Log("Publisher Sent {0} messages to the streaming server.", i);
                            PrintResourceStats();
                        }
                    }
                    sc?.Close();

                    Log("Publisher is finished.");
                }

                long finished = 0;
                internal void SetFinished()
                {
                    Interlocked.Add(ref finished, 1);
                }

                private bool IsFinished()
                {
                    return Interlocked.Read(ref finished) > 0;
                }

            }

            static void Main(string[] args)
            {
                string url = "127.0.0.1";
                string clusterID = "test-cluster";

                if (args.Length > 0) url = args[0];
                if (args.Length > 1) clusterID = args[1];

                Log("==== STAN Client Resource Test ====");
                Log("Using Url: " + url);
                Log("Using ClusterID: " + clusterID);

                Publisher p = new Publisher();
                Subscriber s = new Subscriber();

                void Shutdown()
                {
                    Log("Shutting Down.");
                    s?.SetFinished();
                    p?.SetFinished();
                }

                // Setup a graceful exit for ctrl+c and SIGTERM.
                Console.CancelKeyPress += (o, a) => Shutdown();
                AppDomain.CurrentDomain.ProcessExit += (o, a) => Shutdown();

                try
                {
                    Log("Starting Subscriber.");
                    Task subTask = new Task(() => s.ProcessMsgs(url, clusterID), TaskCreationOptions.LongRunning);
                    subTask.Start();

                    Thread.Sleep(1000);

                    Log("Starting Publisher.");
                    Task pubTask = new Task(() => p.SendMessages(url, clusterID), TaskCreationOptions.LongRunning);
                    pubTask.Start();

                    pubTask.Wait();
                    subTask.Wait();

                    Log("Exiting.");

                }
                catch (Exception e)
                {
                    p?.SetFinished();
                    s?.SetFinished();

                    Log("Error:  " + e.Message);
                }
            }
        }
    }
}
