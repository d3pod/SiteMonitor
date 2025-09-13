using System;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var url = config["Configs:url"];
        var textFind = config["Messages:textFind"];
        var interval = TimeSpan.FromHours(1);

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (monitor-site/1.0)");
            while (true)
            {
                try
                {
                    string html = await client.GetStringAsync(url);
                    if (html.Contains(textFind))
                    {
                        Console.WriteLine(config["Messages:outOfService"] + " -> " + DateTime.Now.ToString());
                        await sendEmail(config);
                    }
                    else
                    {
                        Console.WriteLine(config["Messages:inService"] + " -> " + DateTime.Now.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(config["Messages:notFound"] + $" Error: {ex.Message}");
                }
                await Task.Delay(interval);
            }
        }
    }
    static async Task sendEmail(IConfiguration config)
    {
        string emailFrom = config["Email:emailFrom"];
        string emailTo = config["Email:emailTo"];
        string smtpServer = config["Email:smtpServer"];
        int smtpPort = Convert.ToInt32(config["Email:smtpPort"]);

        try
        {
            var smtp = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(emailFrom, config["Email:emailPassword"]),
                EnableSsl = true
            };

            var mail = new MailMessage();
            mail.From = new MailAddress(config["Email:mailFrom"], "SiteMonitor");
            mail.To.Add(emailTo);
            mail.Subject = config["Email:Subject"];
            mail.Body = config["Email:Body"] + " -> " + DateTime.Now.ToString();

            await smtp.SendMailAsync(mail);
            Console.WriteLine(config["Messages:alertValid"]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(config["Messages:alertInvalid"] + $" -> {ex.Message}");
        }
    }
}