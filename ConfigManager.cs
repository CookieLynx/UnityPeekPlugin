using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityPeekPlugin
{
    static class ConfigManager
    {

        public static string port = "6500";
        public static string IP = "192.168.1.1";


        public static string getConfigDir()
        {
            string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string dllDirectory = System.IO.Path.GetDirectoryName(dllPath);
            string configPath = System.IO.Path.Combine(dllDirectory, "UnityPeekConfig.txt");
            return configPath;
        }


        public static void LoadConfig()
        {
            string configPath = getConfigDir();
            if (System.IO.File.Exists(configPath))
            {
                //Load the config
                string[] lines = System.IO.File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    if(line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        switch (key)
                        {
                            case "port":
                                Plugin.Logger.LogInfo("Setting port to " + value);
                                port = value;
                                break;
                            case "IP":
                                Plugin.Logger.LogInfo("Setting IP to " + value);
                                IP = value;
                                break;
                            default:
                                Plugin.Logger.LogWarning("Unknown config key: " + key);
                                break;
                        }

                    }
                    else
                    {
                        Plugin.Logger.LogWarning("Invalid config line: " + line);
                    }
                }
            }
            else
            {
                Plugin.Logger.LogWarning("Config not found, creating config");
                CreateConfig();
            }


        }

        public static void CreateConfig()
        {
            string configFile = "" +
                "## The IP address the server will run on (the IP you connect to inside UnityPeek)\n"
                + "IP=192.168.1.1\n"
                + "## The port the server will run on\n"
                + "port=6500";


            System.IO.File.WriteAllText(getConfigDir(), configFile);
            Plugin.Logger.LogWarning("Created Config");

        }
    }
}
