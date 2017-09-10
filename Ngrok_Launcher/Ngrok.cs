using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.Serialization;

namespace NgrokLauncher
{
    internal class Ngrok
    {
        private static readonly string NgrokExecutable = "ngrok.exe";
        private static readonly string NgrokYaml = "ngrok.yaml";
        public static readonly string CurrentDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        public static readonly string FileNgrokExecutable = Path.Combine(CurrentDirectory, NgrokExecutable);
        public static readonly string FileConfig = Path.Combine(CurrentDirectory, NgrokYaml);
        private static string LocalHost = "localhost:4040";

        public class Config
        {
            public string authtoken { get; set; }
            public string region { get; set; }
            public bool console_ui { get; set; }
            public string log_level { get; set; }
            public string log_format { get; set; }
            public string log { get; set; }
            public string web_addr { get; set; }
            public bool run_website { get; set; }
            public bool run_ssh { get; set; }
            public Tunnel tunnels { get; set; }
        }

        public class Tunnel
        {
            public Protocol website { get; set; }
            public Protocol ssh { get; set; }
        }

        public class Protocol
        {
            public int addr { get; set; }
            public string proto { get; set; }
            public string auth { get; set; }
        }

        public class Response
        {
            public JsonTunnel[] tunnels { get; set; }
        }

        public class JsonTunnel
        {
            public string name { get; set; }
            public string public_url { get; set; }
            public string proto { get; set; }
        }

        public Ngrok()
        {
            if (!File.Exists(FileConfig))
            {
                var config = new Config
                {
                    authtoken = string.Empty,
                    console_ui = true,
                    region = "us",
                    log_level = "info",
                    log_format = "logfmt",
                    log = "ngrok.log",
                    web_addr = LocalHost,
                    run_website = true,
                    run_ssh = false,
                    tunnels = new Tunnel
                    {
                        website = new Protocol
                        {
                            addr = 80,
                            proto = "http"
                        },
                        ssh = new Protocol
                        {
                            addr = 22,
                            proto = "tcp"
                        }
                    }
                };

                var serializer = new SerializerBuilder().Build();
                var yaml = serializer.Serialize(config);
                File.WriteAllText(FileConfig, yaml);
            }
        }

        public Response GetResponse()
        {
            try
            {
                using (WebClient web = new WebClient())
                {
                    var content = web.DownloadString($"http://{LocalHost}/api/tunnels");
                    return JsonConvert.DeserializeObject<Ngrok.Response>(content);
                }
            }
            catch
            {
                return null;
            }
        }

        public Config Load()
        {
            var yaml = File.ReadAllText(FileConfig);
            var deserializer = new DeserializerBuilder().Build();
            var config = deserializer.Deserialize<Config>(yaml);

            LocalHost = config.web_addr;
            return config;
        }

        public void Save(string token, int http, int tcp, bool website, bool ssh)
        {
            var config = Load();
            config.authtoken = token;
            config.tunnels.website.addr = http;
            config.tunnels.ssh.addr = tcp;
            config.run_website = website;
            config.run_ssh = ssh;

            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(config);
            File.WriteAllText(FileConfig, yaml);
        }

        public async Task Start(int code = 0)
        {
            var exec = new ProcessStartInfo();
            exec.WorkingDirectory = CurrentDirectory;
            exec.FileName = NgrokExecutable;
            exec.CreateNoWindow = true;
            exec.UseShellExecute = false;
            exec.Arguments = $"start -config \"{NgrokYaml}\" ";

            switch (code)
            {
                case 1:
                    exec.Arguments += "website";
                    break;

                case 2:
                    exec.Arguments += "ssh";
                    break;

                default:
                    exec.Arguments += "website ssh";
                    break;
            }

            try
            {
                await Task.Run(() =>
                {
                    var proc = Process.Start(exec);
                    proc.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task Stop()
        {
            await Task.Run(() =>
            {
                Process[] pList = Process.GetProcessesByName("Ngrok");
                foreach (Process p in pList)
                {
                    Console.WriteLine($"Kill: {p.Id}");
                    p.Kill();
                    p.WaitForExit();
                    p.Dispose();
                }
            });
        }
    }
}