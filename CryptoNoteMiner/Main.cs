using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoNoteMiner
{
    public partial class Main : Form
    {
        bool platform64bit;

        string simpleminerPath;
        string simplewalletPath;
        string walletPath;
        string address;

        List<Process> minerProcesses = new List<Process>();

        string miningBtnStart;
        string miningBtnStop;

        SynchronizationContext _syncContext;


        public Main()
        {
            InitializeComponent();

            _syncContext = SynchronizationContext.Current;

            miningBtnStart = buttonStartMining.Text;
            miningBtnStop = "Stop Mining";

            platform64bit = ArchitectureCheck.Is64Bit();

            string platformString = platform64bit ? "64" : "32";

            simpleminerPath = AppDomain.CurrentDomain.BaseDirectory + @"Binaries\" + platformString + @"\simpleminer.exe";
            simplewalletPath = AppDomain.CurrentDomain.BaseDirectory + @"Binaries\" + platformString + @"\simplewallet.exe";

            walletPath = AppDomain.CurrentDomain.BaseDirectory + @"wallet.address.txt";

            if (!File.Exists(simpleminerPath))
            {
                MessageBox.Show("Missing " + simpleminerPath);
            }
            if (!File.Exists(simplewalletPath))
            {
                MessageBox.Show("Missing " + simplewalletPath);
            }

            if (!File.Exists(walletPath))
            {
                MessageBox.Show("Generating new wallet with the password: x");
                GenerateWallet();
            }
            else
            {
                ReadWalletAddress();
            }

            var coresAvailable = Environment.ProcessorCount;
            for (var i = 0; i < coresAvailable; i++)
            {
                string text = (i + 1).ToString();
                if (i+1 == coresAvailable) text += " (max)";
                comboBoxCores.Items.Add(text);
            }

            var coresConfig = INI.Value("cores");
            int coresInt = comboBoxCores.Items.Count - 1;
            if (coresConfig != "")
            {
                int coresParsed;
                var parsed = int.TryParse(coresConfig, out coresParsed);
                if (parsed) coresInt = coresParsed - 1;
                if (coresInt+1 > coresAvailable) coresInt = coresAvailable - 1;

            }
            comboBoxCores.SelectedIndex = coresInt;

            var poolHost = INI.Value("pool_host");
            if (poolHost != ""){
                textBoxPoolHost.Text = poolHost;
            }
            var poolPort = INI.Value("pool_port");
            if (poolPort != "")
            {
                textBoxPoolPort.Text = poolPort;
            }

            Application.ApplicationExit += (s, e) => killMiners();

        }

        void ReadWalletAddress()
        {
            address = File.ReadAllText(walletPath);
            _syncContext.Post(_ =>
            {
                textBoxAddress.Text = address;
            }, null);
            
        }

        void GenerateWallet()
        {
            var args = new [] { 
                "--generate-new-wallet=\"" + AppDomain.CurrentDomain.BaseDirectory + "wallet\"", 
                "--password=x"
            };
            Console.WriteLine(String.Join(" ", args));
            ProcessStartInfo psi = new ProcessStartInfo(simplewalletPath, String.Join(" ", args))
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            Process process = Process.Start(psi);
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                if (!File.Exists(walletPath))
                    MessageBox.Show("Failed to generate new wallet");
                else 
                    ReadWalletAddress();
            };
        }

        void startMiningProcesses()
        {
            var args = String.Join(" ", new[] { 
                "--pool-addr=" + textBoxPoolHost.Text + ':' + textBoxPoolPort.Text,
                "--login=" + address,
                "--pass=x"
            });
            var cores = comboBoxCores.SelectedIndex + 1;
            for (var i = 0; i < cores; i++)
            {
                startMiningProcess(args, i + 1);
            }
        }

        async Task startMiningProcess(string args, int core)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(simpleminerPath, String.Join(" ", args));
            //startInfo.UseShellExecute = false;
            //startInfo.RedirectStandardOutput = true;
            //startInfo.RedirectStandardError = true;
            //startInfo.CreateNoWindow = true;
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;


            Process process = new Process();
            minerProcesses.Add(process);
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => {
                Log("Miner on core " + core + " died");
                minerProcesses.Remove(process);
                if (minerProcesses.Count == 0)
                {
                    _syncContext.Post(_ => {
                        if (buttonStartMining.Text != miningBtnStart)
                            buttonStartMining.PerformClick();
                    }, null);
                }
            };
            //process.OutputDataReceived += (s, a) => Log(a.Data);

            process.Start();
            Log("Miner started on core " + core);
            //process.BeginOutputReadLine();
            //process.BeginErrorReadLine();
            //process.WaitForExit();
        }


        void Log(string text)
        {
            if (text == null) return;
            _syncContext.Post(_ => {
                textBoxLog.AppendText(Environment.NewLine + text);
                textBoxLog.SelectionStart = textBoxLog.Text.Length;
                textBoxLog.ScrollToCaret();
            }, null);
        }

        void SaveINI()
        {
            INI.Config(
                "pool_host", textBoxPoolHost.Text,
                "pool_port", textBoxPoolPort.Text,
                "cores", (comboBoxCores.SelectedIndex + 1).ToString()
            );
        }

        void killMiners()
        {
            foreach (Process process in minerProcesses)
            {
                process.Kill();
            }
            minerProcesses.Clear();
        }

        private void buttonStartMining_Click(object sender, EventArgs e)
        {
            if (buttonStartMining.Text == miningBtnStart)
            {
                SaveINI();
                buttonStartMining.Text = miningBtnStop;
                textBoxPoolHost.Enabled = textBoxPoolPort.Enabled = comboBoxCores.Enabled = false;
                startMiningProcesses();
            }
            else
            {
                buttonStartMining.Text = miningBtnStart;
                textBoxPoolHost.Enabled = textBoxPoolPort.Enabled = comboBoxCores.Enabled = true;
                killMiners();
            }
        }



    }
}
