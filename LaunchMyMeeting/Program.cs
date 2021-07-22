using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LaunchMyMeeting.Models;
using Newtonsoft.Json;
using Wizemen.NET;
using Wizemen.NET.Models;

namespace LaunchMyMeeting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (!File.Exists("config.json"))
            {
                Console.WriteLine();
                Console.WriteLine("Looks like this is your first time here!");
                Console.WriteLine("This is a one time setup, that must be run in order to save a configuration!");

                Configuration config = new Configuration();

                Console.WriteLine();

                Console.Write("What is your wizemen email? ");
                var email = Console.ReadLine() ?? string.Empty;
                var isEmail = Regex.IsMatch(email,
                    @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
                    RegexOptions.IgnoreCase);
                while (!isEmail)
                {
                    Console.WriteLine("The email provided was not in a correct format!");
                    Console.Write("What is your wizemen email? ");
                    email = Console.ReadLine() ?? string.Empty;
                    isEmail = Regex.IsMatch(email,
                        @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
                        RegexOptions.IgnoreCase);
                }

                Console.Write("What is your wizemen password?: ");
                var password = Console.ReadLine();

                Console.Write("What is your school code? (PWS, PSN, PSG): ");
                SchoolCode schoolCode;
                while (!Enum.TryParse(Console.ReadLine()?.ToUpper(), out schoolCode))
                {
                    Console.WriteLine("Incorrect school code!");
                    Console.Write("What is your school code? (PWS, PSN, PSG): ");
                }

                config.Credentials = new Credentials(email, password, schoolCode);

                await File.WriteAllTextAsync("config.json", JsonConvert.SerializeObject(config));

                Console.WriteLine("Credentials saved successfully!");
                Console.WriteLine();

                Console.WriteLine("The program will now ask you for your preferences.");
                Console.Write(
                    "Would you like to auto launch meetings in your browser? (Once you open this program, the meeting will automatically open in your browser)\n[y\\n]: ");
                var input = Console.ReadLine();
                config.AutoLaunch = !string.IsNullOrWhiteSpace(input) && input.ToUpper()[0] == 'Y';
                await File.WriteAllTextAsync("config.json", JsonConvert.SerializeObject(config));

                Console.WriteLine("Setup complete!");
                Console.WriteLine();
                Console.Write("Would you like to launch your meeting now? [y/n]: ");
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.ToUpper()[0] != 'Y')
                {
                    return;
                }
            }

            await Meetings();
        }

        private static async Task Meetings()
        {
            var config = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync("config.json"));
            var client = new WizemenClient(config.Credentials);
            await client.StartAsync();
            var meetings = await client.GetMeetingsAsync();

            Console.WriteLine($"Found {meetings.Count} TOTAL meetings");

            // var currTime = DateTime.Now;
            var currTime = new DateTime(2021, 7, 22, 8, 26, 0);

            meetings = meetings
                .Where(meeting => meeting.StartTime.DayOfYear == currTime.DayOfYear)
                .ToList();

            var finalMeetings = (from meeting in meetings
                let minutesDiff = (currTime - meeting.StartTime).TotalMinutes
                where minutesDiff < meeting.Duration - 10 && minutesDiff >= -5
                select meeting).ToList();

            switch (finalMeetings.Count)
            {
                case <= 0:
                    Console.WriteLine("No scheduled for now.");
                    break;

                case 1:
                {
                    var m = finalMeetings[0];
                    if (config.AutoLaunch)
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo
                            {
                                UseShellExecute = true, FileName = m.JoinUrl
                            });
                    }
                    else
                    {
                        Console.WriteLine(
                            $"{m.Topic}, {m.Host}\n{m.Agenda}\n{m.JoinUrl}\nId: {m.Id}\nPassword: {m.Password}");
                    }

                    break;
                }
                default:
                {
                    Console.WriteLine("Multiple meetings found. They are printed below:");
                    foreach (var m in finalMeetings)
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            $"{m.Topic}, {m.Host}\n{m.Agenda}\n{m.JoinUrl}\nId: {m.Id}\nPassword: {m.Password}");
                    }
                    break;
                }
            }
        }
    }
}