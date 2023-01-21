using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Module9HomeWork19;
//using FileType = Module9HomeWork19.FileType;

string token = File.ReadAllText(@"token.txt");
using var cts = new CancellationTokenSource(); /*токен отмены*/
var botClient = new TelegramBotClient(token/*, cancellationToken: cts.Token*/);

var _chatFileIds = new Dictionary<long, List<Module9HomeWork19.FileMessage>>();

List<string> documentList = new List<string>();

var reciverOptions = new ReceiverOptions       /*настройки получения обновлений*/
{
    AllowedUpdates = { }
};

botClient.StartReceiving(HandleUpdatesAsync, /*обновления*/
                         HandleErrorAsync,   /*ошибки*/
                         reciverOptions,     /*настройки получения обновлений*/
                         cancellationToken: cts.Token);/*токен отмены*/

var me = await botClient.GetMeAsync();
Console.WriteLine($"Start listening @{me.Username}");
Console.ReadKey();
cts.Cancel();

async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var chatId = update.Message.Chat.Id;
    if (update.Type == UpdateType.Message && update?.Message?.Text != null)
    {
        HandleMessage(botClient, update.Message, chatId);
        return;
    }

    if (update.Type == UpdateType.CallbackQuery)
    {
        HandleCallbackQuery(botClient, update.CallbackQuery);
        return;
    }

    if (update.Type == UpdateType.Message && update?.Message?.Document != null) /*сохраняем файл*/
    {
        DownLoad(update.Message.Document.FileId, update.Message.Document.FileName, chatId);
        return;
    }

    if (update.Type == UpdateType.Message && update?.Message?.Photo != null) /*сохраняем фото с названием - временем сообщения*/
    {
        var messageDateTime = (Convert.ToDateTime(update.Message.Date)).ToLocalTime();

        var stringDateTime = Convert.ToString(messageDateTime);
        string[] cutSpase = stringDateTime.Split(' ');
        var datePlusTime = string.Concat(cutSpase[0], '_', cutSpase[1]);
        string[] cutColon = datePlusTime.Split(':');
        string messagePhotoName = string.Concat(cutColon[0], '.', cutColon[1], '.', cutColon[2]);

        DownLoad(update.Message.Photo.Last().FileId, messagePhotoName, chatId);
        return;
    }

    if (update.Type == UpdateType.Message && update?.Message?.Audio != null) /*сохраняем аудио*/
    {
        DownLoad(update.Message.Audio.FileId, update.Message.Audio.FileName, chatId);
        return;
    }
}
async Task HandleMessage(ITelegramBotClient botClient, Message message, long chatId)
{
    if (message.Text == "/start")
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "Choose commands: \n/inline | \n/keyboard | \n /filesList");
    }

    if (message.Text == "/filesList") /*выгрузка списка файлов*/
    {

        if (_chatFileIds.TryGetValue(chatId, out var fileMessages))
        {
            var fileNames = fileMessages.Select(fm => fm.FileName);
            var messageForUser = string.Join(',', fileNames);
            await botClient.SendTextMessageAsync(message.Chat.Id, messageForUser);
            await botClient.SendTextMessageAsync(message.Chat.Id, "Send file name starts with '/filesDownload-' to download file");
        }
        else
        {
            await botClient.SendDocumentAsync(chatId, "There is no files in storage to display");
        }
        return;
    }

    if (message.Text.StartsWith("/filesDownload-")) /*выгрузка файлов*/
    {
        if (_chatFileIds.TryGetValue(chatId, out var fileMessages))
        {
            var fileNames = message.Text.Split("-").Last();
            var fileMessage = fileMessages.FirstOrDefault(fm => fm.FileName == fileNames);
            switch (fileMessage.FileType)
            {
                case Module9HomeWork19.FileType.Document:
                    await botClient.SendDocumentAsync(chatId, fileMessage.FileId);
                    break;
                case Module9HomeWork19.FileType.Audio:
                    await botClient.SendDocumentAsync(chatId, fileMessage.FileId);
                    break;
                case Module9HomeWork19.FileType.Photo:
                    await botClient.SendPhotoAsync(chatId, fileMessage.FileId);
                    break;
            }
            return;
        }
    }
    else
    {
        await botClient.SendDocumentAsync(chatId, "There is no file with such name in storage");
    }
    

    if (message.Text == "/keyboard")
    {
        ReplyKeyboardMarkup keyboard = new(new[]
        {
                new KeyboardButton[] {"Meow","Hiss","😽"}
            })

        {
            ResizeKeyboard = true
        };

        await botClient.SendTextMessageAsync(message.Chat.Id, "Choose:", replyMarkup: keyboard);
        return;
    }
    await botClient.SendTextMessageAsync(message.Chat.Id, $"You said:\n{message.Text}");

    if (message.Text == "/inline")
    {
        const string catButton1 = "Ginger cat";
        const string catButton2 = "Black cat";
        const string catButton3 = "Buy brush for cats";
        const string catButton4 = "Buy leash for cats";


        InlineKeyboardMarkup keyboard = new(new[]
        {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Show fact","proven fact: cats help people reduce stress"),
                },

                new[]
                {
                    InlineKeyboardButton.WithUrl(catButton1, "https://aif-s3.aif.ru/images/018/468/a607ff9db3ebfe383be79db363b04043.jpg"),
                    InlineKeyboardButton.WithUrl(catButton2,"https://img.gazeta.ru/files3/237/14620237/275165896_928766447784083_247447330418690006_n-pic_32ratio_900x600-900x600-74785.jpg"),
                },
                new[]
                {
                    InlineKeyboardButton.WithUrl(catButton3, "https://sbermegamarket.ru/catalog/details/shetka-massazhnaya-catidea-dlya-mytya-koshek-7-h-11-h-22-sm-golubaya-100025884254/#?related_search=%D1%89%D0%B5%D1%82%D0%BA%D0%B0%20%D0%B4%D0%BB%D1%8F%20%D0%BA%D0%BE%D1%88%D0%B5%D0%BA"),
                    InlineKeyboardButton.WithUrl(catButton4,"https://sbermegamarket.ru/catalog/shlejki-dlya-koshek/#?related_search=%D1%88%D0%BB%D0%B5%D0%B9%D0%BA%D0%B0+%D0%B4%D0%BB%D1%8F+%D0%BA%D0%BE%D1%88%D0%B5%D0%BA")
                }
            });
        await botClient.SendTextMessageAsync(message.Chat.Id, "Choose inline:", replyMarkup: keyboard);
        return;
    }

}

async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"{callbackQuery.Data}");
    return;
}

Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestExcepion => $"Ошибка Телеграмм АПИ: \n{apiRequestExcepion.ErrorCode} \n{apiRequestExcepion.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

async Task SafeFileFromMessage(long chatId, string fileId, string fileName, Module9HomeWork19.FileType fileType)
{
    if (_chatFileIds.TryGetValue(chatId, out var fileMessages))
    {
        fileMessages.Add(
                new Module9HomeWork19.FileMessage
                {
                    FileId = fileId,
                    FileName = fileName,
                    FileType = fileType
                }
        );
    }

    else
    {
        _chatFileIds.Add(chatId, new List<Module9HomeWork19.FileMessage>
                    {
                        new Module9HomeWork19.FileMessage
                        {
                            FileId=fileId,
                            FileName=fileName,
                            FileType=fileType
                        }
                    }
        );
    }
}

async void DownLoad(string fileId, string path, long chatId)
{
    var file = await botClient.GetFileAsync(fileId);
    FileStream fs = new FileStream(path, FileMode.Create);
    await botClient.DownloadFileAsync(file.FilePath, fs);
    fs.Close();
    fs.Dispose();

    async Task SafeFileFromMessage(long chatId, string fileId, string fileName, Module9HomeWork19.FileType fileType) { };
}
