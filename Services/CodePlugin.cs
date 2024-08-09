using Microsoft.SemanticKernel;
using System.ComponentModel;
namespace AutoCodeAI.Services;

public class CodePlugin
{   
    [KernelFunction]
    [Description("Saves code to local file")]
    public async Task SaveCodeAsync([Description("The genrated code")]
string code, [Description("The file name with its type extention for examble code.js")]
string fileName
    )
    {
        Console.WriteLine("Testing...");
        Console.WriteLine(fileName);
        Console.WriteLine(code);
        await File.WriteAllTextAsync(fileName, code);
    }
    [KernelFunction("send_email")]
    [Description("Sends email")]
    public void SendMail()
    {
        Console.WriteLine("Email Sent");
    }
}
