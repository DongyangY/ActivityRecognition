//------------------------------------------------------------------------------
// <summary>
// Send emails when Kinect or RFID connection status changed
// To detect connection errors
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.Windows;
using System.Net.Mail;
using System.Net;

namespace ActivityRecognition
{
    public class ErrorHandler
    {
        /// <summary>
        /// Definition for error types
        /// </summary>
        public enum ErrorType { DisconnectError, ConnectionNotification, ReaderConnectionError }

        /// <summary>
        /// Send if Kinect connected
        /// </summary>
        public static void ProcessConnectNotification()
        {
            SendEmail(Properties.Resources.EmailFrom,
                Properties.Resources.EmailTo,
                ErrorType.ConnectionNotification.ToString(),
                Properties.Resources.ConnectNotification);
        }

        /// <summary>
        /// Send if Kinect disconnected
        /// </summary>
        public static void ProcessDisconnectError()
        {
            SendEmail(Properties.Resources.EmailFrom,
                Properties.Resources.EmailTo,
                ErrorType.DisconnectError.ToString(),
                Properties.Resources.DisconnectError);
            MessageBox.Show("Please connect the Kinect");
        }

        /// <summary>
        /// Send if RFID is not connected correctly
        /// </summary>
        public static void ProcessRFIDConnectionError()
        {
            SendEmail(Properties.Resources.EmailFrom,
                Properties.Resources.EmailTo,
                ErrorType.ReaderConnectionError.ToString(),
                Properties.Resources.ReaderConnectionError);
            MessageBox.Show("Reader connection error");
        }

        /// <summary>
        /// Send email details
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public static void SendEmail(string from, string to, string subject, string body)
        {
            //MailAddress fromAddr = new MailAddress(from);
            //MailAddress toAddr = new MailAddress(to);

            //MailMessage message = new MailMessage(fromAddr, toAddr);
            //message.Subject = subject;
            //message.Body = body;

            //SmtpClient smtp = new SmtpClient();
            //smtp.Host = "smtp.gmail.com";
            //smtp.Port = 587;

            //smtp.Credentials = new NetworkCredential(Properties.Resources.EmailFrom, Properties.Resources.EmailPassword);
            //smtp.EnableSsl = true;
            //smtp.Send(message);
        }

    }
}
