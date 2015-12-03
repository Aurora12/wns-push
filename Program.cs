using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WNSPushNotification
{
  /*
  Utility code only (arguments, reading files, etc.). 
  All code relevant to WNS is located in WNSPushNotificationApp class.
  */
  class Program
  {
    static string[] Types = new []{"wns/toast", "wns/badge", "wns/tile", "wns/raw"};
    
    static Arguments Args;
    static WNSPushNotificationApp App;

    public class Arguments
    {
      public string Type;
      public string ChannelUri;
      public string ContentPath;
      public string Sid;
      public string Secret;

      public string Content;

      public string Print()
      {
        return
          "Notification type: " + Type +
          "\n" + "Channel URI: " + ChannelUri +
          "\n" + "Content file path: " + ContentPath +
          "\n" + "Sid: " + Sid +
          "\n" + "Secret: " + Secret +
          "\n" + "Notification content:\n" + Content;
      }
    }

    public class Result
    {
      public string ErrorMessage;
    }

    static void Main(string[] args)
    {
      if (args.Length == 1) {
        try {
          string content = File.ReadAllText(args[0]);
          args = new Regex("\\s+", RegexOptions.Singleline).Split(content);
        } catch (Exception e) {
          Console.WriteLine("Failed to read params file: {0}", e.Message);
        }
      }
      Args = CollectArguments(args);
      if (Args == null) {
        Console.WriteLine(PrintUsage());
      } else {
        TaskCompletionSource<Result> waiter = new TaskCompletionSource<Result>();
        Console.WriteLine("Starting...");

        LaunchApp(Args, waiter);
        
        waiter.Task.Wait(10000);
        
        Console.WriteLine();

        if (waiter.Task.IsCompleted) {
          var result = waiter.Task.Result;
          if (string.IsNullOrWhiteSpace(result.ErrorMessage)) {
            Console.WriteLine("SUCCESS!");
          } else {
            Console.WriteLine("FAILURE! {0}", result.ErrorMessage);
          }
        } else {
          Console.WriteLine("FAILURE! Failed to complete deffered job: {0}", waiter.Task.Status);
        }
      }
    }

    private static async Task LaunchApp(Arguments args, TaskCompletionSource<Result> completionTrigger)
    {
      // Reading notification content from file

      string errorMessage = null;
      try {
        using (var stream = new StreamReader(args.ContentPath, Encoding.UTF8)) {
          args.Content = await stream.ReadToEndAsync().ConfigureAwait(false);
        }
      } catch (Exception e) {
        errorMessage = e.Message;
      }

      if (string.IsNullOrWhiteSpace(Args.Content)) {
        completionTrigger.SetResult(new Program.Result(){
          ErrorMessage = string.Format("Failed to read notification content.\n{0}", errorMessage)
        });
      } else {
        // Code relevant to WNS starts executing here
        App = new WNSPushNotificationApp();
        App.Start(Args, completionTrigger);
      }
    }

    static Arguments CollectArguments(string[] args)
    {
      var result = new Arguments();
      int count = 0;
      string name = null;
      for (int i = 0; i < args.Length; i++) {
        if (args[i].StartsWith("-")) {
          name = args[i].Substring(1).ToLower();
        } else {
          if (string.IsNullOrWhiteSpace(name)) {
            Debug.WriteLine("Unhandled parameter {0}", args[i]);
          } else {
            if (name == "type") {
              if (Types.Contains(args[i])) {
                result.Type = args[i];
                count++;
              }
            } else if (name == "channel") {
              result.ChannelUri = args[i];
              count++;
            } else if (name == "content") {
              result.ContentPath = args[i];
              count++;
            } else if (name == "sid") {
              result.Sid = args[i];
              count++;
            } else if (name == "secret") {
              result.Secret = args[i];
              count++;
            } else {
              Debug.WriteLine("Unsupported parameter {0}", name);
            }
          }
        }
      }

      if (count == 5) {
        return result;
      }
      return null;
    }

    static string PrintUsage()
    {
      return
        "Usage:" +
        "\n\nWNSPushNotification.exe -type type -channel -content file_path -sid package_sid -secret client_secret" +
        "\n\nOR" +
        "\n\nWNSPushNotification.exe params_file_path" +
        "\n\nParameters:" +
        "\n\n" + "-type: " + string.Join(", ", Types) +
        "\n" + "-channel: Client WNS channel uri. Get this from client app." +
        "\n" + "          Looks like https://db5.notify.windows.com/?token=AwYAfhG7...uw63YQ%3d" +
        "\n" + "-content: Path to file containning notification content text (xml or plain text)." +
        "\n" + "-sid: Package sid from live services." +
        "\n" + "      Looks like ms-app://s-1-15-2-379812837-1203...658-254873585" +
        "\n" + "-secret: Client secret from live services." +
        "\n" + "         Looks like SM5DkfddlfkIOldkf+shjJhlsjkJhd" +
        "\n\n" + "OR" +
        "\n\n" + "params_file_path path to file containing all of the above parameters";
    }
  }
}
