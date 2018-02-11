using System;
using System.Collections;
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
        readonly bool platform64bit;

        string simplewalletPath;
        string cpuminerPath;

        string walletPath;
        string address;

        readonly List<Process> minerProcesses = new List<Process>();

        readonly string miningBtnStart;
        readonly string miningBtnStop;

        readonly SynchronizationContext _syncContext;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        public Main()
        {
            InitializeComponent();

            _syncContext = SynchronizationContext.Current;

            miningBtnStart = buttonStartMining.Text;
            miningBtnStop = "Stop Mining";

            platform64bit = ArchitectureCheck.Is64Bit();

            string platformString = platform64bit ? "64bit" : "32bit";

            simplewalletPath = AppDomain.CurrentDomain.BaseDirectory + @"binaries\simplewallet\" + platformString + @"\simplewallet.exe";
            cpuminerPath = AppDomain.CurrentDomain.BaseDirectory + @"binaries\cpuminer\" + platformString + @"\minerd.exe";

            walletPath = AppDomain.CurrentDomain.BaseDirectory + @"wallet.address.txt";

            if (!File.Exists(simplewalletPath))
            {
                MessageBox.Show("Missing " + simplewalletPath);
                Process.GetCurrentProcess().Kill();
            }

            if (!File.Exists(cpuminerPath))
            {
                MessageBox.Show("Missing " + cpuminerPath);
                Process.GetCurrentProcess().Kill();
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
                if (i+1== coresAvailable)
                {
                    text += " (max)";
                }

                comboBoxCores.Items.Add(text);
            }

            var coresConfig = INI.Value("cores");
            var coresInt = comboBoxCores.Items.Count - 1;
            if (coresConfig != "")
            {
                var parsed = int.TryParse(coresConfig, out int coresParsed);
                if (parsed)
                {
                    coresInt = coresParsed - 1;
                }

                if (coresInt+1> coresAvailable)
                {
                    coresInt = coresAvailable - 1;
                }
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

            Application.ApplicationExit += (s, e) => KillMiners();

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
                {
                    MessageBox.Show("Failed to generate new wallet");
                }
                else
                {
                    ReadWalletAddress();
                }
            };
        }

        void StartMiningProcesses()
        {
            var args = new ArrayList(new[] {
                "-a cryptonight",
                "-o stratum+tcp://" + textBoxPoolHost.Text + ':' + textBoxPoolPort.Text,
                "-u " + address,
                "-p x"
            });
            var cores = comboBoxCores.SelectedIndex + 1;
            if (cores != comboBoxCores.Items.Count)
            {
                args.Add("-t " + cores);
            }

            StartMiningProcess((string[])args.ToArray(typeof(string)), cores);

        }

        void StartMiningProcess(string[] args, int cores)
        {
            Process process = new Process();
            minerProcesses.Add(process);
            ProcessStartInfo startInfo = new ProcessStartInfo(cpuminerPath, string.Join(" ", args));
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => {
                Log("Miner died");
                minerProcesses.Remove(process);
                if (minerProcesses.Count == 0)
                {
                    _syncContext.Post(_ => {
                        if (buttonStartMining.Text != miningBtnStart)
                        {
                            buttonStartMining.PerformClick();
                        }
                    }, null);
                }
            };

            process.Start();

            var ptr = IntPtr.Zero;
            while ((ptr = process.MainWindowHandle) == IntPtr.Zero || process.HasExited)
            {
                ;
            }

            SetParent(process.MainWindowHandle, panel1.Handle);
            MoveWindow(process.MainWindowHandle, 0, 0, panel1.Width, panel1.Height - 20, true);

            Log("Miner started on " + cores + " cores");
        }


        void Log(string text)
        {
            if (text == null)
            {
                return;
            }

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

        void KillMiners()
        {
            foreach (Process process in minerProcesses)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            minerProcesses.Clear();
        }

        private void ButtonStartMining_Click(object sender, EventArgs e)
        {
            if (buttonStartMining.Text == miningBtnStart)
            {
                SaveINI();
                buttonStartMining.Text = miningBtnStop;
                textBoxPoolHost.Enabled = textBoxPoolPort.Enabled = comboBoxCores.Enabled = false;
                StartMiningProcesses();
            }
            else
            {
                buttonStartMining.Text = miningBtnStart;
                textBoxPoolHost.Enabled = textBoxPoolPort.Enabled = comboBoxCores.Enabled = true;
                KillMiners();
            }
        }



    }
}
