using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace SMTP_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // initialize smtp
            SmtpClient smtp = new SmtpClient();
            smtp.DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis;

            // initialize mail
            MailMessage mailMsg = new MailMessage();
            mailMsg.From = new MailAddress("dztsese@163.com");
            mailMsg.To.Add(new MailAddress("yangkaixi007@sina.com"));
            mailMsg.Subject = "Test Email";

            // we want send html code so that we can see the rich text, i.e css style
            mailMsg.IsBodyHtml = true;

            mailMsg.Body = @"<tb>
                                 <tr>
                                     <td>Column #1</td>
                                     <td>Column #2</td>
                                 </tr>
                                 <tr>
                                     <td>Lucas Luo</td>
                                     <td>Bill Gates</td>
                                 </tr>";

            // here we send the email, you can also use SendAsync to give contro back to your caller
            smtp.Send(mailMsg);

            Console.WriteLine("Finish email sending......");
            Console.ReadLine();
        }
    }
}
