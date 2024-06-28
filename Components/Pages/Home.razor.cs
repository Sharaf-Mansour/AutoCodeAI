using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;
using AutoCodeAI.Services;
namespace AutoCodeAI.Components.Pages;

public partial class Home
{
    string? Message { get; set; }
    string? Response { get; set; }
    int Num = 0;
    public string ParseHtmlContent(string input)
    {
        Num++;
        var pattern = @"```(.*?)\n(.*?)```";
        var regex = new Regex(pattern, RegexOptions.Singleline);
        var result = regex.Replace(input, match =>
        {
            var lang = match.Groups[1].Value.Trim();
            var code = match.Groups[2].Value.Trim();
            return $"<div>\r\n<button class=\"btn btn-info\" onclick=\"copyToClipboard('id{Num}')\">Copy</button>\r\n   <pre><code id='id{Num}' class=\"{lang}\">{System.Net.WebUtility.HtmlEncode(code)}</code></pre></div>";
        });
        return (result);
    }
    async ValueTask<string> SeniorAI(string Message)
    {
        var CodeAgent = AIAgents.SrCodeAgent(Message);
        var chatMessage = "";
        Response += Environment.NewLine;
        await foreach (var content in CodeAgent)
        {
            chatMessage += content?.Content;
            Response += content?.Content;
            await InvokeAsync(StateHasChanged);
            await _js.InvokeVoidAsync("scrollToEnd");

        }
        AIAgents.SrChatMessages.AddAssistantMessage(chatMessage);
        Response = ParseHtmlContent(Response);

        return chatMessage;
    }
    async ValueTask<string> JunoirAI(string Message)
    {
        var CodeAgent = AIAgents.JrCodeAgent(Message);
        var chatMessage = "";
        Response += Environment.NewLine;
        await foreach (var content in CodeAgent)
        {
            chatMessage += content?.Content;
            Response += content?.Content;
            await InvokeAsync(StateHasChanged);
            await _js.InvokeVoidAsync("scrollToEnd");

        }
        AIAgents.JrChatMessages.AddAssistantMessage(chatMessage);
        Response = ParseHtmlContent(Response);
        return chatMessage;
    }


    async Task DoAiStuff()
    {
        Response = "";
        var SrAnswer = await SeniorAI(Message ?? "");
        await _js.InvokeVoidAsync("scrollToEnd");
        static bool IsTaskComplete(string answer) => answer.Contains(AIAgents.TASK_COMPLETE_PHRASE, StringComparison.CurrentCultureIgnoreCase);
        while (true)
        {
            Response += Environment.NewLine + "<hr/>" + Environment.NewLine + "JR AI: ";
            var JrAnswer = await JunoirAI(SrAnswer);
            await _js.InvokeVoidAsync("scrollToEnd");

            if (IsTaskComplete(JrAnswer)) break;
            else Console.WriteLine("False");
            Response += Environment.NewLine + "<hr/>" + Environment.NewLine + "SR AI: ";
            SrAnswer = await SeniorAI(JrAnswer);
            await _js.InvokeVoidAsync("scrollToEnd");

            if (IsTaskComplete(SrAnswer)) break;
            else Console.WriteLine("False");
        }

        await InvokeAsync(StateHasChanged);
        var timer = new System.Timers.Timer(20);
        timer.Elapsed += async (sender, e) =>
        {
            await _js.InvokeVoidAsync("highlightSnippet");

            timer.Stop();
        };
        timer.Start();
        await Task.Delay(1000);
        await _js.InvokeVoidAsync("scrollToEnd");

    }
}