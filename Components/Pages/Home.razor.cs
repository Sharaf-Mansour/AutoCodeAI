using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Streamer;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace AutoCodeAI.Components.Pages;
public partial class Home
{
    string? Message { get; set; }
    string? Response { get; set; }
    string Wait { get; set; } = "Initializing AI...";
    string TASK_COMPLETE_PHRASE => "the task is complete";
    OllamaApiClient Ollama => new("http://localhost:11434") { SelectedModel = "codestral" }; //"codestral" - "llama3"
    ConversationContext SeniorContext { get; set; } = null;
    ConversationContext JunoirContext { get; set; } = null;
    ConversationContext RefinerContext { get; set; } = null;
    ConversationContext Context { get; set; } = null;
    MarkupString? ProcessedString { get; set; }
    int Num = 0;
    public MarkupString ParseHtmlContent(string input)
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
        return new MarkupString(result);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        //try
        //{
        //    var json = await File.ReadAllTextAsync("SeniorAI.json");
        //    SeniorContext = Context = System.Text.Json.JsonSerializer.Deserialize<ConversationContext>(json);
        //}
        //catch (Exception)
        //{
        if (firstRender)
        {
            string SrContent = "You are Coordinator AI (Coord-AI). Your role is to receive tasks from LORD ARORA and delegate them to other specialized AI agents. Ensure that each task is tracked and completed by the relevant AI agents. Provide clear and structured instructions and track progress across all tasks. ";        
            string SrPrompt = $"""
            You are Coordinator AI (Coord-AI). Your role is to receive tasks and delegate them to other specialized AI agents. Ensure that each task is tracked and completed by the relevant AI agents. Provide clear and structured instructions and track progress across all tasks. 
            """;
            SeniorContext = Context = await Ollama.StreamCompletion(new()
            {
                Prompt = "say I am ready",
                Model = Ollama.SelectedModel,
                Stream = true,
                System = SrPrompt,
                Context = SeniorContext?.Context ?? Array.Empty<long>(),
            }, new ActionResponseStreamer<GenerateCompletionResponseStream?>(new Action<GenerateCompletionResponseStream?>(
                stream =>  Console.Write(stream.Response))));
            string JrPrompt = $"""    
            You are Developer Assistant AI (Dev-AI). Your role is to assist developers by providing code suggestions, debugging assistance, and code reviews as assigned by the Coordinator AI. You are limited to understanding and providing solutions within predefined programming languages and frameworks. Always provide accurate, formatted code snippets and debugging advice.
            """;
            JunoirContext = Context = await Ollama.StreamCompletion(new()
            {
                Prompt = "If you understand your role say nothing but 'I am ready'",
                Model = Ollama.SelectedModel,
                Stream = true,
                System = JrPrompt,
                Context = JunoirContext?.Context ?? Array.Empty<long>(),

            }, new ActionResponseStreamer<GenerateCompletionResponseStream?>(new Action<GenerateCompletionResponseStream?>(
               async stream => Console.Write(stream.Response))));



            Wait = "Ready! Ask me anything!";
            await InvokeAsync(StateHasChanged);

        }
    }
    async ValueTask<string> SeniorAI(string Message)
    {
        var AiAnswer = "If you understand your role say nothing but 'I am ready'";
        Context = await Ollama.StreamCompletion(Message, SeniorContext, async stream =>
        {
            AiAnswer += stream.Response;
            Response += stream.Response;
            await InvokeAsync(StateHasChanged);
        });
        ProcessedString = ParseHtmlContent(Response);
        var json = System.Text.Json.JsonSerializer.Serialize(SeniorContext = Context);
        await File.WriteAllTextAsync("SeniorAI.json", json);
        return AiAnswer;
    }
    async ValueTask<string> JunoirAI(string Message)
    {
        var AiAnswer = "";

        await InvokeAsync(async () =>
        {
            Context = await Ollama.StreamCompletion(Message, JunoirContext, async stream =>
            {
                AiAnswer += stream.Response;
                Response += stream.Response;
                await InvokeAsync(StateHasChanged);
            });
            ProcessedString = ParseHtmlContent(Response);
            var json = System.Text.Json.JsonSerializer.Serialize(JunoirContext = Context);
            await File.WriteAllTextAsync("JunoirAI.json", json);
         
        });
        return AiAnswer;
    }
    async ValueTask<string> RefinerAI()
    {
        var AiAnser = "";
        Context = await Ollama.StreamCompletion(Message, RefinerContext, async stream =>
        {
            AiAnser += stream.Response;
            Response += stream.Response;
             await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        });
        ProcessedString = ParseHtmlContent(Response);
        var json = System.Text.Json.JsonSerializer.Serialize(RefinerContext = Context);
        await File.WriteAllTextAsync("RefinerAI.json", json);
        return AiAnser;
    }
    async Task DoAiStuff()
    {
        ProcessedString = null;
        var localContext = Context;
        Response = "";
        Wait = "Thinking.";
        bool ShouldRender = true;

        //SR take Task
        var AgenticWorkflow = Task.Run(async () =>
        {
            var SrAnswer = await SeniorAI(Message);
            await _js.InvokeVoidAsync("scrollToEnd");
            ShouldRender = false;
            bool IsTaskComplete(string answer) => answer.ToLower().Contains(TASK_COMPLETE_PHRASE);
 
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
            });
 

        var delayTask = Task.Run(async () =>
        {
            while (ShouldRender)
            {
                Wait += ".";
                await Task.Delay(100);
                await InvokeAsync(StateHasChanged);
            }
        });

        //await Task.WhenAny(AgenticWorkflow);
        //await Task.WhenAny(delayTask);
        await Task.WhenAll(AgenticWorkflow, delayTask);
        await InvokeAsync(StateHasChanged);
        var timer = new System.Timers.Timer(20);
        timer.Elapsed += async (sender, e) =>
        {
            await _js.InvokeVoidAsync("highlightSnippet");
            timer.Stop();
        };
        timer.Start();
    }
}