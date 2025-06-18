using Amqp;
using System;

namespace LargerSimonSays;

public class MessagePipeline(SimonSaysGame game)
{
    public void Start()
    {
        var connection = new Connection(new Address(""));
        var session = new Session(connection);
        var receiveLink = new ReceiverLink(session, "Simon Says receiver link", "simonsays_puzzle");

        receiveLink.Start(1, (link, msg) =>
        {
            var messageType = ((string)msg?.MessageAnnotations["messagetype"]).ToLower();

            switch (messageType)
            {
                case "largerSimonSays.messages.showsequence":
                    game.ShowSequence();
                    break;

                case "largerSimonSays.messages.showsolved":
                    game.ShowSolved();
                    break;

                case "largerSimonSays.messages.resetpattern":
                    game.ResetPattern();
                    break;

                case "largerSimonSays.messages.showfailed":
                    game.ShowFailed();
                    break;

                case "largerSimonSays.messages.captureinput":
                    game.CaptureInput(5); // todo: replace with actual button number from message
                    break;

                default:
                    Console.WriteLine($"Unknown message type: {messageType}");
                    break;
            }
        });
    }
}
