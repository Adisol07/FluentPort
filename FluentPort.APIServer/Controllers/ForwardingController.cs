using FluentPort.SDK;
using FluentPort.APIServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FluentPort.APIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class ForwardingController : ControllerBase
{
    [HttpGet("assign_server")]
    public async Task<IActionResult> AssignServer()
    {
        try
        {
#if DEBUG
            return Ok(new APIResult("Success", APIResultStatus.Success, JsonSerializer.Serialize(ForwardingModel.Servers[0])));
#else
            string ip = get_client_ip();
            Server server = await ForwardingModel.SelectServer(ip);
            if (server == null!)
            {
                Logger.Log(new Log(LogLevels.Warning, "Forwarding.AssignServer", "All servers are full"));
                return Ok(new APIResult("Full", APIResultStatus.Failed, ""));
            }
            Logger.Log(new Log(LogLevels.Information, "Forwarding.AssignServer", "Selected server for " + ip));
            return Ok(new APIResult("Success", APIResultStatus.Success, JsonSerializer.Serialize(server)));
#endif
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Forwarding.AssignServer", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("save_port")]
    public IActionResult SavePort([FromBody] TunnelSaveRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Request!.Token) && AuthModel.TokenToID.TryGetValue(request.Request.Token, out string? token_id))
            {
                Account account = Account.Load(token_id);
                if (account.Tunnels!.ContainsKey(request.Info!.ID!))
                {
                    account.Tunnels![request.Info.ID!] = request.Info!;
                    account.Tunnels![request.Info.ID!].RemoteAddress = "";
                    account.Tunnels![request.Info.ID!].RemotePort = 0;
                    account.Save();
                    Logger.Log(new Log(LogLevels.Information, "Forwarding.SavePort", "Saved port for: " + request.Request.Token));
                    return Ok(new APIResult("Success", APIResultStatus.Success, request.Info.ID!));
                }
                else
                {
                    if (account.Tunnels!.Count >= ForwardingModel.MaxCreatedTunnels)
                    {
                        Logger.Log(new Log(LogLevels.Warning, "Forwarding.SavePort", "Limit of tunnels reached for: " + request.Request.Token));
                        return Ok(new APIResult("Limit", APIResultStatus.Failed, ForwardingModel.MaxCreatedTunnels.ToString()));
                    }

                    TunnelInfo info = new TunnelInfo(Guid.NewGuid().ToString(), request.Info.LocalAddress!, request.Info.RemoteAddress!, request.Info.LocalPort, request.Info.RemotePort, DateTimeOffset.Now);
                    account.Tunnels?.Add(info.ID!, info);
                    account.Save();
                    Logger.Log(new Log(LogLevels.Information, "Forwarding.SavePort", "Created port for: " + request.Request.Token));
                    return Ok(new APIResult("Success", APIResultStatus.Success, info.ID!));
                }
            }

            Logger.Log(new Log(LogLevels.Error, "Forwarding.SavePort", "Token is invalid: " + request.Request.Token));
            return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, "Token is invalid"));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Forwarding.CreatePort", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("remove_port")]
    public IActionResult RemovePort([FromBody] TunnelRemovalRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Request!.Token) && AuthModel.TokenToID.TryGetValue(request.Request.Token, out string? token_id))
            {
                Account account = Account.Load(token_id);
                account.Tunnels?.Remove(request.ID!);
                account.Save();
                Logger.Log(new Log(LogLevels.Information, "Forwarding.RemovePort", "Removed port for: " + request.Request.Token));
                return Ok(new APIResult("Success", APIResultStatus.Success));
            }

            Logger.Log(new Log(LogLevels.Error, "Forwarding.RemovePort", "Token is invalid: " + request.Request.Token));
            return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, "Token is invalid"));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Forwarding.RemovePort", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("tunnel_connected_on_server")]
    public IActionResult TunnelConnectedOnServer([FromBody] ServerOperationRequest request)
    {
        try
        {
            Server server = ForwardingModel.IncrementTunnelConnected(request);
            if (server == null!)
            {
                Logger.Log(new Log(LogLevels.Error, "Forwarding.TunnelConnectedOnServer", "Could not find server: " + request.Target));
                return Ok(new APIResult("Failed", APIResultStatus.Failed, "Could not find server"));
            }
            Logger.Log(new Log(LogLevels.Information, "Forwarding.TunnelConnectedOnServer", "Tunnel connected on server: " + request.Target));
            return Ok(new APIResult("Success", APIResultStatus.Success, "Active tunnels: " + server.ActiveTunnels));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Forwarding.TunnelConnectedOnServer", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("tunnel_disconnected_from_server")]
    public IActionResult TunnelDisconnectedFromServer([FromBody] ServerOperationRequest request)
    {
        try
        {
            Server server = ForwardingModel.DecrementTunnelConnected(request);
            if (server == null!)
            {
                Logger.Log(new Log(LogLevels.Error, "Forwarding.TunnelDisconnectedOnServer", "Could not find server: " + request.Target));
                return Ok(new APIResult("Failed", APIResultStatus.Failed, "Could not find server"));
            }
            Logger.Log(new Log(LogLevels.Information, "Forwarding.TunnelDisconnectedFromServer", "Tunnel disconnected from server: " + request.Target));
            return Ok(new APIResult("Success", APIResultStatus.Success, "Active tunnels: " + server.ActiveTunnels));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Forwarding.TunnelDisconnectedFromServer", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    private string get_client_ip()
    {
        var remote_ip = HttpContext.Connection.RemoteIpAddress;
        if (remote_ip != null && remote_ip.IsIPv4MappedToIPv6)
        {
            return remote_ip.MapToIPv4().ToString();
        }
        else return remote_ip!.ToString();
    }
}
