using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Service_contract;
using Microsoft.Extensions.Configuration;

namespace ServiceLayer.Services.Auth.Smtp
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;
        public EmailSender(IConfiguration cfg) { _cfg = cfg; }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var smtp = _cfg.GetSection("Smtp");
            var host = smtp["Host"];
            var port = int.Parse(smtp["Port"] ?? "587");
            var fromEmail = smtp["Email"];
            var password = smtp["Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };
            var message = new MailMessage(fromEmail, to, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(message);
        }
    }
}
