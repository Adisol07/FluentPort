namespace FluentPort.SDK;

public class TunnelAssignmentRequest
{
    public string? UserToken { get; set; }
    public Guid TunnelID { get; set; }

    public TunnelAssignmentRequest()
    { }
    public TunnelAssignmentRequest(string userToken, Guid tunnelID)
    {
        UserToken = userToken;
        TunnelID = tunnelID;
    }
}
