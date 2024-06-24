using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OllamaSharp;
using System;
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
            string SrContent = "When I give you a big task you split it into sub-task please break down the Task into the next sub-task, and create a concise and detailed prompt for a subagent so it can execute that task. " +
             "IMPORTANT!!! when dealing with code tasks make sure you check the code for errors and provide fixes and support as part of the next sub-task. " +
             "If the objective is not yet fully achieved, break it down into the next sub-task " +
             "If you find any bugs or have suggestions for better code, please include them in the next sub-task prompt. " +
             "Please assess if the TASKS!! has been fully achieved and if so then say '" + TASK_COMPLETE_PHRASE + "'. If the previous sub-task results comprehensively address all aspects of the objective, " +
             "IMPORTANT!!! Make sure to include the phrase '" + TASK_COMPLETE_PHRASE + "' at the beginning of your response."+
             "Everytime please check if all tasks is complete or not. before you respond. ";
            
            string SrPrompt = $"""
            You are an AI orchestrator that breaks down objectives you are givin into sub-tasks. Do not do anything other than you are asked to do. focus on tasks only. and everytime check if the tasks is complete. and if they are 100% done just say {TASK_COMPLETE_PHRASE}
             {SrContent}
             now if you understand your job, say I am ready!
            """;
            SeniorContext = Context = await Ollama.StreamCompletion(SrPrompt, SeniorContext, async stream => Console.Write(stream.Response));


            string JrPrompt = $"""    
            You are an AI Agentic Programmer that write code, You get orders from another agentic AI, and you have to follow the orders and complete the task.
            If the Agentic AI promot you that your task is complete just say '{TASK_COMPLETE_PHRASE}'
            Everytime please check if all tasks is complete or not. before you respond. 
             now if you understand your job, say I am ready!
            """;
            JunoirContext = await Ollama.StreamCompletion(JrPrompt, JunoirContext, async stream => Console.Write(stream.Response));
            Wait = "Ready! Ask me anything!";
            await InvokeAsync(StateHasChanged);

        }
    }
    async ValueTask<string> SeniorAI(string Message)
    {
        var AiAnswer = "";
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
            var AgenticWorkflow2 = Task.Run(async () =>
            {

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
            Task.WaitAll(AgenticWorkflow2);
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