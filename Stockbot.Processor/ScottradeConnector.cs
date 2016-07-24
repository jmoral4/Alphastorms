using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.AddOns.FormProcessor;

namespace Stockbot.Processor
{
    public class ScottradeConnector
    {
        public bool TryConnectToService(string endpointAddress, string username, string password)
        {
            bool result = false;

            try
            {
                
                FormProcessor   p = new FormProcessor();
                //p.Web.UsingCache = true;
                p.Web.UseCookies = true;
                //secureUserAccountNumber
                //secureUserPassword
                //Form1


                //stockcharts test
                //https://stockcharts.com/scripts/php/dblogin.php
                //form_UserID
                //form_UserPassword
                //id=loginform
                //name=login
                Form form = p.GetForm(
                    "https://stockcharts.com/scripts/php/dblogin.php",
                    "//form[@id='loginform']",
                    FormQueryModeEnum.Nested
                 );

                //http://stockcharts.com/def/servlet/Favorites.CServlet
                
                form["form_UserID"].SetAttributeValue("value", username);
                form["form_UserPassword"].SetAttributeValue("value", password);

                HtmlDocument doc = p.SubmitForm(form);
                Thread.Sleep(100);
                HtmlDocument resp = p.Web.Load("http://stockcharts.com/def/servlet/Favorites.CServlet");
                var node = resp.DocumentNode;

                var welcome = resp.DocumentNode.SelectSingleNode("//h2[@class='entry-header']");
                string welcomemsg = "UNKNOWN NODE";
                if (welcome != null)
                    welcomemsg = welcome.InnerText;

                welcomemsg = System.Web.HttpUtility.HtmlDecode(welcomemsg);

                Console.WriteLine("Server returned:" + welcomemsg);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return result;
        }

    }
}
