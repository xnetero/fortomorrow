using todolist.Models;
using MimeKit;
using Microsoft.EntityFrameworkCore.Query.Internal;
using MailKit.Net.Smtp;

namespace todolist.UtilityService
{
    public class EmailService : IEmailService
    {

        private readonly IConfiguration _config;


        public EmailService() { }

        public void SendEmail(EmailModel emailmodel)
        {

            var emailMessage = new MimeMessage();
            var from = _config["EmailSettings:From"];
            emailMessage.From.Add(new MailboxAddress("lets progrm", from));
            emailMessage.To.Add(new MailboxAddress(emailmodel.To, emailmodel.To));

            emailMessage.Subject = emailmodel.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = string.Format(emailmodel.Content)
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(_config[      "EmailSettings:SmtpServer"     ], 587,true) ;
                    client.Authenticate(_config["EmailSettings:From"], _config["EmailSettings:From"]);
                    client.Send(emailMessage);
                    // Rest of the code here
                }
                catch(Exception ex) 
                {
                    throw;

                }
                finally { 

                    client.Disconnect(true);
                    client.Dispose();
                
                }
            }





        }
    }
}
