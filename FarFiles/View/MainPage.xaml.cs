using Microsoft.JSInterop;

namespace FarFiles.View;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }


    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("app://message"))
        {
            e.Cancel = true;

            var uri = new Uri(e.Url);
            var json = Uri.UnescapeDataString(System.Web.HttpUtility.ParseQueryString(uri.Query).Get("json"));

            var message = JsonSerializer.Deserialize<WebRTCMessage>(json);

            switch (message.Type)
            {
                case "signal":
                    // Forward to other peer
                    break;
                case "connected":
                    Console.WriteLine("TURN connection ready!");
                    break;
                case "data-binary":
                    byte[] binary = message.Data.EnumerateArray().Select(x => (byte)x.GetInt32()).ToArray();
                    Console.WriteLine("Binary message: " + BitConverter.ToString(binary));
                    break;
                case "error":
                    Console.WriteLine("WebRTC error: " + message.Message);
                    break;
            }
        }
    }

    private async Task StartWebRTC()
    {
        var turn = new
        {
            urls = new[] { "turn:turn.metered.ca:80" },
            username = "your-user",
            credential = "your-pass"
        };

        await JS.InvokeVoidAsync("setupWebRTC", true, turn, null);
    }

    private async Task SendBinaryTest()
    {
        byte[] data = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray();
        string base64 = Convert.ToBase64String(data);
        await JS.InvokeVoidAsync("sendBinaryToPeer", base64);
    }



    [JSInvokable]
    public static Task ReceiveFromJs(string json)
    {
        var doc = JsonDocument.Parse(json);
        string type = doc.RootElement.GetProperty("type").GetString();

        switch (type)
        {
            case "connected":
                Console.WriteLine("Connected!");
                break;
            case "signal":
                // Relay signal to peer via backend
                var signalData = doc.RootElement.GetProperty("data").ToString();
                Console.WriteLine("Signal to send to peer: " + signalData);
                break;
            case "data-binary":
                var base64 = doc.RootElement.GetProperty("base64").GetString();
                var binary = Convert.FromBase64String(base64);
                Console.WriteLine("Received " + binary.Length + " bytes of binary data.");
                break;
            case "error":
                Console.WriteLine("WebRTC error: " + doc.RootElement.GetProperty("message").GetString());
                break;
        }

        return Task.CompletedTask;
    }
}
