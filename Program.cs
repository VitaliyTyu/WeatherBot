using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;

class Program
{
  private static readonly string BotToken = "";
  private static readonly string OpenWeatherMapApiKey = "";
  private static readonly TelegramBotClient BotClient = new TelegramBotClient(BotToken);

  static async Task Main(string[] args)
  {

    // https://api.openweathermap.org/data/2.5/weather?q=Perm&appid=a9298b65485c86d0f05e0375e0559f60&units=metric&lang=ru
    // var weatherPerm = await GetWeatherAsync("Moscow");
    // Console.WriteLine(weatherPerm);

    using var cts = new CancellationTokenSource();

    BotClient.StartReceiving(
        HandleUpdateAsync,
        HandleErrorAsync,
        cancellationToken: cts.Token
    );

    Console.WriteLine("Bot is running. Press any key to exit.");
    Console.ReadKey();

    cts.Cancel();
  }

  private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
  {
    if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
      return;

    var chatId = update.Message.Chat.Id;
    var messageText = update.Message.Text!.Trim();

    Console.WriteLine($"Received a message from {update.Message.Chat.Username}: {messageText}");

    string responseMessage = await GetWeatherAsync(messageText);

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: responseMessage,
        cancellationToken: cancellationToken
    );
  }

  private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
  {
    var errorMessage = exception switch
    {
      ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
      _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
  }

  private static async Task<string> GetWeatherAsync(string cityName)
  {
    string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={OpenWeatherMapApiKey}&units=metric&lang=ru";

    using HttpClient client = new HttpClient();

    try
    {
      HttpResponseMessage response = await client.GetAsync(apiUrl);
      response.EnsureSuccessStatusCode();

      string responseBody = await response.Content.ReadAsStringAsync();
      JObject weatherData = JObject.Parse(responseBody);

      string description = weatherData["weather"][0]["description"].ToString();
      double temperature = double.Parse(weatherData["main"]["temp"].ToString());
      double feelsLike = double.Parse(weatherData["main"]["feels_like"].ToString());

      return $"Погода в {cityName}: \n- {description}\n - Температура: {temperature}°C\n - Ощущается как: {feelsLike}°C";
    }
    catch (HttpRequestException)
    {
      return "Не удалось получить данные о погоде. Проверьте название города или попробуйте позже.";
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Ошибка: {ex.Message}");
      return "Произошла ошибка. Попробуйте позже.";
    }
  }
}
