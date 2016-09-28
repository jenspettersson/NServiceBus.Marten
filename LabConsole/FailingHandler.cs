using System;
using System.Threading.Tasks;
using NServiceBus;

namespace LabConsole
{
    public class FailMiserably : ICommand
    {
        public string Message { get; set; }

        public FailMiserably(string message)
        {
            Message = message;
        }
    }

    public class FailingHandler : IHandleMessages<FailMiserably>
    {
        public async Task Handle(FailMiserably message, IMessageHandlerContext context)
        {
            await Console.Out.WriteLineAsync($"Going to fail with message: {message.Message}");
            throw new Exception("I am failing... I am faaaailing!");
        }
    }
}