using System.Diagnostics;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LogParser
{
    public partial class FormMain : Form
    {
        private static string _file = @"C:\Windows\CCM\Logs\SensorManagedProvider.Log";
        private static long _lastPosition;
        private static bool _doNotRead;

        private FileSystemWatcher _watcher;

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            if (!File.Exists(_file)) return;

            FileInfo fi = new FileInfo(_file);

            string? path = fi.Directory?.FullName;
            string filename = fi.Name;

            // start reading at end of file
            _lastPosition = fi.Length;

            if (path != null)
            {
                _watcher = new FileSystemWatcher(path);

                // maximize buffer
                _watcher.InternalBufferSize = 65536;

                _watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;

                _watcher.Changed += Watcher_Changed;
                _watcher.Created += Watcher_Created;
                _watcher.Deleted += Watcher_Deleted;
                _watcher.Renamed += Watcher_Renamed;
                _watcher.Error += Watcher_Error;

                _watcher.Filter = filename;

                _watcher.IncludeSubdirectories = false;
                _watcher.EnableRaisingEvents = true;
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            _doNotRead = true;

            Console.WriteLine(e.GetException().Message);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            _doNotRead = true;

            ResetPointer();
            ResetLog();

            Console.WriteLine($"Renamed from {e.OldName} to {e.Name}");
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            _doNotRead = true;

            ResetPointer();
            ResetLog();

            Console.WriteLine($"{e.Name} deleted!");
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _doNotRead = false;

            Console.WriteLine($"{e.Name} changed!");

            Tail();
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            _doNotRead = false;

            ResetPointer();

            Console.WriteLine($"{e.Name} created!");
        }

        private void ResetPointer()
        {
            _lastPosition = 0;
        }

        private void ResetLog()
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    textBoxOutput.Text = "";
                });
            }
            else
            {
                textBoxOutput.Text = "";
            }
        }

        private void Tail()
        {
            if (_doNotRead) return;

            using (var fs = new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(_lastPosition, SeekOrigin.Begin);
                var buffer = new byte[1024];

                while (true)
                {
                    _lastPosition = fs.Position;

                    Debug.WriteLine($"Position: {fs.Position}");

                    var bytesRead = fs.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("End of File");

                        break;
                    }

                    var text = ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead);

                    Add(text);
                }
            }
        }

        private void Add(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    textBoxOutput.Text += text;

                    textBoxOutput.SelectionStart = textBoxOutput.TextLength - 1;
                    textBoxOutput.SelectionLength = 0;

                    textBoxOutput.ScrollToCaret();
                });
            }
            else
            {
                textBoxOutput.Text += text;

                textBoxOutput.SelectionStart = textBoxOutput.TextLength - 1;
                textBoxOutput.SelectionLength = 0;

                textBoxOutput.ScrollToCaret();
            }
        }
    }
}