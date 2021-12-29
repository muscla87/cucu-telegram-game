using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using CucuGame = Cucu.Engine.Game;

namespace Cucu
{
    public class UpdateHttpHandler
    {
        private readonly CucuGame _game;

        public UpdateHttpHandler(CucuGame game)
        {
            _game = game;
        }

        [FunctionName("Update")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP triggfgfdgger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Update update =  JsonSerializer.Deserialize<Update>(requestBody);

            var botClient = new TelegramBotClient(ConfigurationSettings.BotApiKey);
            if(update.Message != null) 
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                await _game.LoadGameStateAsync(chatId);

                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said from Function:\n" + messageText);
            }


            return new OkResult();
        }
    }
}
