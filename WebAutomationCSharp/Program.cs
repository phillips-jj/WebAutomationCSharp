using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHtmlUnit;    //Nuget package

/*  This sample code demonstrates (using my version of the AuthenticationAndAccessRestrictions CherryPy web server sample) how to use HtmlUnit in C#.NET 
 *  to automate a web browser.  Obviously, if your web service as a REST API, you would just call that API directly - HtmlUnit is good for when you want
 *  to exercise the web UI directly, or if you have no REST API.
 *  
 *  Ensure that my version of the AuthenticationAndAccessRestrictions CherryPy web server sample is running correctly first by performing the same actions as
 *  below, but in a browser pointed to http://127.0.0.1:8080.
 *  
 *  NB - most error handling has been omitted for brevity/clarity.
 */

namespace WebAutomationCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            /*  Set the client to mimic Firefox.  By default I believe it mimics IE, and that implementation has some issues.  For example, the call below
             *  to generateLink.Click() fails when the default WebClient constructor is called (IE), but succeeds when Firefox is mimiced.  This seems to be a
             *  jQuery problem - see https://github.com/playframework/playframework/issues/5050 et. al.
             * 
             */
            WebClient webClient = new WebClient(BrowserVersion.FIREFOX_38);

            // This actually takes us to the /index page in test-login.py, but since we're not logged in yet, it will bump us to /auth/login,
            // then back to /index once the login is successful.
            NHtmlUnit.Html.HtmlPage currentPage = webClient.GetHtmlPage("http://127.0.0.1:8080");


            /*
             * At this point we assume that we're on /index, but it's always a good idea to check.  Two ways to do this are:
             * 
             * 1. currentPage.BaseURL.ToString() (assuming you're confident of the page's content once you know the URL)
             * 2. currentPage.AsText(), and then parse the string to determine the page's content, looking for specific text - especially 
             *      useful if the page is dynamically-generated, and you don't know if what you're looking for is present (or present yet).
             */


            NHtmlUnit.Html.HtmlTextInput username = (NHtmlUnit.Html.HtmlTextInput)currentPage.GetElementByName("username");
            username.Text = "joe";

            NHtmlUnit.Html.HtmlPasswordInput password = (NHtmlUnit.Html.HtmlPasswordInput)currentPage.GetElementByName("password");
            password.Text = "secret";

            /*
             * The login button has no name or ID (see auth.py, get_loginform) - so we need to iterate through each HTML element to find it.  We could give it
             * a name or ID, but the code below highlights how to handle a situation where we have no control over the web page.
             */
            NHtmlUnit.Html.HtmlSubmitInput loginButton = null;
            bool foundLoginButton = false;
            foreach (NHtmlUnit.Html.HtmlElement el in currentPage.TabbableElements)
            {
                if (!foundLoginButton)
                {
                    try
                    {
                        //try casting each element to a submit button - the right one will succeed, the wrong ones will throw exceptions
                        loginButton = (NHtmlUnit.Html.HtmlSubmitInput)el;
                        foundLoginButton = true;
                    }
                    catch (Exception /*ee*/)
                    {
                        //keep looking for it
                    }
                }
            }

            currentPage = (NHtmlUnit.Html.HtmlPage)loginButton.Click();

            //The Click() method returns when the next page (which is /index) is loaded.  We'll go to the "Generate Random String" link and "click" it.


            // It would be simpler to just call GetHtmlPage("/generate"), but this highlights a useful feature - moving to a page where we know the link
            // text, but not the URL.  If you know the URL but not the text, use GetAnchorByHref()
            NHtmlUnit.Html.HtmlAnchor generateLink = currentPage.GetAnchorByText("Generate Random String");          
            currentPage = (NHtmlUnit.Html.HtmlPage)generateLink.Click();
            //The Click() method returns when the next page (which is /generate) is loaded.  

            NHtmlUnit.Html.HtmlButton generateButton = (NHtmlUnit.Html.HtmlButton)currentPage.GetElementById("generate-string");
            generateButton.Click();

            NHtmlUnit.Html.HtmlTextInput randomString = (NHtmlUnit.Html.HtmlTextInput)currentPage.GetElementById("random-string");
            string theRandomString = randomString.Text;
            /*
             *  Ah-ha!!!  At this point, the value of theRandomString will be empty, even though one might think that it should contain the random string.
             *  Even if you (in the debugger) stop on a breakpoint immediately after generateButton.Click() for hours, the string will be empty.
             *  We need to tell the web client to wait (e.g. for 500ms) for the Javascript to complete - this function *also* updates the state of the HtmlPage
             *  object (and child objects like text inputs), so we can read the random string.
             */
            webClient.WaitForBackgroundJavaScript(500);
            theRandomString= randomString.Text;

            Console.WriteLine(theRandomString);

            webClient.GetHtmlPage("/auth/logout");

            currentPage.CleanUp();
            webClient.Close();
        }
    }
}
