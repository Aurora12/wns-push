using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace WNSPushNotification
{
  class WNSPushNotificationApp
  {
    TaskCompletionSource<Program.Result> CompletionTrigger;

    public async void Start(Program.Arguments args, TaskCompletionSource<Program.Result> completionTrigger)
    {
      Console.WriteLine(args.Print());
      CompletionTrigger = completionTrigger;

      string token = null, errorMessage = null;

      // 1. Requesting token

      try {
        token = await RequestToken(args.Secret, args.Sid);
      } catch (Exception e) {
        errorMessage = e.Message;
      }

      if (string.IsNullOrWhiteSpace(token)) {
        CompletionTrigger.SetResult(new Program.Result(){
          ErrorMessage = string.Format("Failed to get access token.\n{0}", errorMessage)
        });
      } else {

        // 2. Sending notification

        try {
          errorMessage = await SendNotification(args.Type, args.ChannelUri, token, args.Content);
        } catch (Exception e) {
          errorMessage = e.Message;
        }

        if (string.IsNullOrWhiteSpace(errorMessage)) {
          CompletionTrigger.SetResult(new Program.Result());
        } else {
          CompletionTrigger.SetResult(new Program.Result(){
            ErrorMessage = string.Format("Failed to send notification.\n{0}", errorMessage)
          });
        }
      }
    }

    async Task<string> RequestToken(string secret, string sid)
    {
      string accessTokenUrl = "https://login.live.com/accesstoken.srf";
      string[] query = new string[] {
        "grant_type=client_credentials",
        "scope=notify.windows.com",
        "client_secret=" + WebUtility.UrlEncode(secret),
        "client_id="     + WebUtility.UrlEncode(sid)
      };
      byte[] postData = Encoding.Default.GetBytes(
        string.Join("&", query)
      );

      var request = WebRequest.Create(accessTokenUrl);
      request.ContentType = "application/x-www-form-urlencoded";
      request.Method = "POST";
      request.ContentLength = postData.Length;

      Console.WriteLine("\nSending token query: {0} bytes.", postData.Length);
      Console.WriteLine("Getting token response.");

      var stream = request.GetRequestStream();
      await stream.WriteAsync(postData, 0, postData.Length).ConfigureAwait(false);
      stream.Close();

      var response = await request.GetResponseAsync().ConfigureAwait(false);

      Console.WriteLine("\nToken response headers:\n{0}", PrintHeaders(response.Headers));
      
      if (response.ContentLength > 0) {
        Console.WriteLine("\nReading response...");

        var serializer = new DataContractJsonSerializer(typeof(TokenResponse));
        var responseData = serializer.ReadObject(response.GetResponseStream()) as TokenResponse;
        if (responseData != null) {
          Console.WriteLine(
            "Token query response:\nAccess token: {0}\nTokenType: {1}\nExpires in: {2}",
            responseData.AccessToken,
            responseData.TokenType,
            responseData.ExpiresIn
          );
          return responseData.AccessToken;
        } else {
          Console.WriteLine("Failed to deserialize token response.");
        }
      }

      return null;
    }

    async Task<string> SendNotification(string type, string channel, string token, string content)
    {
      byte[] postData = Encoding.Default.GetBytes(content);

      var request = WebRequest.Create(channel);
      request.ContentType = type == "wns/raw" ? "text/plain" : "text/xml";
      request.Method = "POST";
      request.ContentLength = postData.Length;

      request.Headers.Add("X-MessageID", Guid.NewGuid().ToString());
      request.Headers.Add("X-NotificationClass", GetNotificationClass(type).ToString());
      request.Headers.Add("X-WNS-Type", type);
      request.Headers.Add("Authorization", "Bearer " + token);

      Console.WriteLine("\nSending notification query: {0} bytes.", postData.Length);
      Console.WriteLine("Getting notification response.");      

      var stream = request.GetRequestStream();
      await stream.WriteAsync(postData, 0, postData.Length).ConfigureAwait(false);
      stream.Close();

      var response = await request.GetResponseAsync().ConfigureAwait(false);

      Console.WriteLine("\nNotification response headers:\n{0}", PrintHeaders(response.Headers));

      if (response.ContentLength > 0) {
        Console.WriteLine("\nReading response...");
        var reader = new StreamReader(response.GetResponseStream());
        var responseContent = await reader.ReadToEndAsync();
        Console.WriteLine("Notification query response:\n{0}", responseContent);
      }

      var status = response.Headers["x-wns-notificationstatus"];
      if (status == "received") {
        return null;
      } else {
        return string.Format("Problem occured! Status: {0}", status);
      }
    }

    int GetNotificationClass(string type)
    {
      if (type == "wns/toast") {
        return 2;
      } else if (type == "wns/raw") {
        return 3;
      } else {
        return 1;
      }
    }

    string PrintHeaders(WebHeaderCollection headers)
    {
      string result = "\t";
      var names = headers.AllKeys.ToList();
      names.Sort();
      foreach (var name in names) {
        if (result.Length > 0) {
          result += "\n\t";
        }
        result += name + ": " + headers[name];
      }
      return result;
    }
  }

  [DataContract]
  class TokenResponse
  {
    [DataMember(Name="access_token")] 
    public string AccessToken;
    
    [DataMember(Name="token_type")] 
    public string TokenType;
    
    [DataMember(Name="expires_in")] 
    public string ExpiresIn;
  }
}
