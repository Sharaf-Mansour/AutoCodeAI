using Microsoft.SemanticKernel;
using System.ComponentModel;
namespace AutoCodeAI.Services;

public class CodePlugin
{
    [KernelFunction("save_code")]
    [Description("Saves code to local file")]
    public async Task SaveCodeAsync(string code,string fileName
    )
    {
        Console.WriteLine("Did this shit work?");
        Console.WriteLine(fileName);
        Console.WriteLine(code);
        await File.WriteAllTextAsync(fileName, code);
    }
    [KernelFunction("send_email")]
    [Description("Sends email")]
    public void SendMail()
    {
        Console.WriteLine("go fuck yourself");
    }
}