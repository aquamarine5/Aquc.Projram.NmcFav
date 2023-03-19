using Microsoft.Extensions.Hosting;
using Huanent.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Win32;
using System.CommandLine;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Security.Principal;

namespace Aquc.Projram.NmcFav
{
    public class NmcFavProgram
    {
        static async Task Main(string[] args)
        {
            using var host = new HostBuilder()
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Debug);
                    builder.AddFilter<FileLoggerProvider>(level => level >= LogLevel.Debug);
                    builder.AddConsole();
                    builder.AddFile();
                })
                .Build();
            var _logger= host.Services.GetRequiredService<ILogger<NmcFavProgram>>();
            var requestAdminOption = new Option<bool>("--request-admin");
            var executeCommand = new Command("execute");
            var executeAdminCommand = new Command("executeAdmin")
            {
                requestAdminOption
            };
            var registerCommand = new Command("register");
            var root = new RootCommand()
            {
                registerCommand,
                executeAdminCommand,
                executeCommand
            };
            var dictionary = new Dictionary<string, string>
            {
                { ".aac","cloudmusic.aac" },
                {".flac","cloudmusic.flac" },
                {".m4a","cloudmusic.m4a" },
                {".mp3","cloudmusic.mp3" },
                {".ogg","cloudmusic.ogg" },
                {".wav","cloudmusic.wav" },
                {".wma","cloudmusic.wma" }
            };
            registerCommand.SetHandler(async () =>
            {
                using var process2 = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "schtasks",
                        Arguments = $"/Create /F /SC daily /st 20:00 /TR \"'{Environment.ProcessPath + "' execute"}\" /TN \"Aquacore\\Aquc.NmcFavExecute\"",
                        CreateNoWindow = true,
                        
                    }
                };
                process2.Start();
                await process2.WaitForExitAsync();
                _logger.LogInformation("Success schedule nmcfav-schedule-execute");
            });
            executeCommand.SetHandler(() =>
            {
                var classes = Registry.CurrentUser.OpenSubKey("SOFTWARE")?.OpenSubKey("Classes")!;
                var keyArray = dictionary.Keys.ToArray();
                var valueArray=dictionary.Values.ToArray();
                for (int i = 0; i < dictionary.Count; i++)
                {
                    var key = classes.OpenSubKey(keyArray[i], true);
                    key!.SetValue("", valueArray[i]);
                    _logger.LogDebug("{key} is edited to {value}", keyArray[i], valueArray[i]);
                }
            });
            executeAdminCommand.SetHandler((bool requestAdmin) =>
            {
                var id = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(id);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    var classes = Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("Classes")!;
                    var keyArray = dictionary.Keys.ToArray();
                    var valueArray = dictionary.Values.ToArray();
                    for (int i = 0; i < dictionary.Count; i++)
                    {
                        var key = classes.OpenSubKey(keyArray[i], true);
                        key!.SetValue("", valueArray[i]);
                        _logger.LogDebug("{key} is edited to {value}", keyArray[i], valueArray[i]);
                    }
                }
                else
                {
                    if (requestAdmin)
                    {
                        _logger.LogError("Failed to get admin permission.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = Environment.ProcessPath,
                                Arguments = "executeAdmin --request-admin",
                                Verb = "runas"
                            }
                        }.Start();
                        _logger.LogWarning("no admin");
                        Environment.Exit(0);
                    }
                }
            },requestAdminOption);
            if (args.Length == 0) args = new string[] { "executeAdmin" };
            try
            {
                await root.InvokeAsync(args);
            }
            catch(Exception ex)
            {
                _logger.LogError("{ex} {msg}", ex.Message, ex.StackTrace);
            }
        }
        //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\网易云音乐
    }
}