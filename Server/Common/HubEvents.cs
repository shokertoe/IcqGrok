namespace ICQ.Server.Common;

/// <summary>SignalR client method names (avoid magic strings in Hub).</summary>
public static class HubEvents
{
    public const string Error = "Error";
    public const string ReceiveMessage = "ReceiveMessage";
    public const string UserTyping = "UserTyping";
    public const string UserStatusChanged = "UserStatusChanged";
    public const string CallOffer = "CallOffer";
    public const string CallAnswer = "CallAnswer";
    public const string IceCandidate = "IceCandidate";
    public const string CallHangup = "CallHangup";
    public const string CallReject = "CallReject";
}
