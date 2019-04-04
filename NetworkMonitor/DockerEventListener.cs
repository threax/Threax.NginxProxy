using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    enum DockerEvent
    {
        NetworkEvent,
        ProcessEnded
    }

    class DockerEventListener : IDisposable
    {
        private Thread workThread;
        private Process process;
        private String network;
        private AsyncQueue<DockerEvent> eventQueue = new AsyncQueue<DockerEvent>();

        public DockerEventListener(String network)
        {
            this.network = network;
        }

        public void Dispose()
        {
            if(process != null)
            {
                process.Kill();
                process.Dispose();
            }
        }

        public void Start()
        {
            if (workThread != null)
            {
                throw new InvalidOperationException("Docker event listener already started.");
            }

            workThread = new Thread(new ThreadStart(() =>
            {
                var startInfo = new ProcessStartInfo("docker", $"events -f network={network}");
                startInfo.RedirectStandardError = true;

                startInfo.RedirectStandardOutput = true;
                this.process = Process.Start(startInfo);
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Console.Error.WriteLine("Docker error " + e.Data);
                    }
                };
                process.OutputDataReceived += (s, e) =>
                {
                    this.eventQueue.Enqueue(DockerEvent.NetworkEvent);
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine("Docker event " + e.Data);
                    }
                };
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
                this.eventQueue.Enqueue(DockerEvent.ProcessEnded);
            }));
            workThread.Start();
        }

        public Task<DockerEvent> GetNextTask()
        {
            return eventQueue.DequeueAsync();
        }
    }
}
