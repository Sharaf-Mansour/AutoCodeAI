using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
namespace AutoCodeAI.Services;
#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
public class AIAgents
{
    public static string TASK_COMPLETE_PHRASE => "TASK_GOGO_GAGA_WEWE_LALA";
    public ChatHistory SrChatMessages = new(
    $"You are an AI orchestrator that breaks down objectives you are givin into sub-tasks. Do not do anything other than you are asked to do. focus on tasks only. and everytime check if the tasks is complete" +
    "When I give you a big task you split it into sub-task please break down the Task into the next sub-task, and create a concise and detailed prompt for a subagent so it can execute that task. " +
    "IMPORTANT!!! when dealing with code tasks make sure you check the code for errors and provide fixes and support as part of the next sub-task. " +
    "If the objective is not yet fully achieved, break it down into the next sub-task " +
    "If you find any bugs or have suggestions for better code, please include them in the next sub-task prompt. " +
    "Please assess If the previous sub-task results comprehensively address all aspects of the objective, " +
    $"You Must say the exact phrase  hi to indicate that ALL THE TASKS  is complete.");
    public ChatHistory JrChatMessages = new($"""
    You are a friendly assistant who likes to follow the rules. You will complete required steps
    and request approval before taking any consequential actions. If the user doesn't provide
    enough information for you to complete a task, you will keep asking questions until you have
    enough information to complete the task.
    """);
    static string ModelId => "codestral";
    static Uri Endpoint => new("http://localhost:11434");
    static IKernelBuilder Builder => Kernel.CreateBuilder();
    public Kernel SrSoftwareAgenet => Builder.AddOpenAIChatCompletion(ModelId,Endpoint,"").Build();
    public Kernel JrSoftwareAgenet => Builder.AddOpenAIChatCompletion(ModelId,Endpoint,"").Build();
    public IAsyncEnumerable<StreamingChatMessageContent> SrCodeAgent(string Message)
    {
        SrChatMessages.AddUserMessage(Message);
        var result = SrSoftwareAgenet.GetRequiredService<IChatCompletionService>()
            .GetStreamingChatMessageContentsAsync(SrChatMessages,null,SrSoftwareAgenet);
        return result;
    }
    public IAsyncEnumerable<StreamingChatMessageContent> JrCodeAgent(string Message)
    {
        JrChatMessages.AddUserMessage(Message);
        var result = SrSoftwareAgenet.GetRequiredService<IChatCompletionService>()
            .GetStreamingChatMessageContentsAsync(JrChatMessages, null, JrSoftwareAgenet);
        return result;
    }

}