using FluentPort.SDK;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using FluentPort.APIServer.Models;

namespace FluentPort.APIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Token) && AuthModel.TokenToID.TryGetValue(request.Token, out string? token_id))
            {
                return Authenticate(token_id);
            }
            else if (!string.IsNullOrEmpty(request.Username) && AuthModel.UsernameToID.TryGetValue(request.Username, out string? username_id))
            {
                return Authenticate(username_id, request.PasswordHash);
            }
            else if (!string.IsNullOrEmpty(request.Email) && AuthModel.EmailToID.TryGetValue(request.Email, out string? email_id))
            {
                return Authenticate(email_id, request.PasswordHash);
            }
            return Ok(new APIResult("Invalid credentials", APIResultStatus.Failed));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.Login", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    private IActionResult Authenticate(string id, string? password = null)
    {
        Account account = Account.Load(id);

        if (password != null && account.PasswordHash != null && !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
        {
            return Ok(new APIResult("Invalid credentials", APIResultStatus.Failed));
        }

        account.Token = AuthModel.GenerateToken();
        account.Save();

        Logger.Log(new Log(LogLevels.Information, "Auth.Login", "Successful login: " + account.Token));
        return Ok(new APIResult("Success", APIResultStatus.Success, account.Token));
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.PasswordHash))
            {
                Logger.Log(new Log(LogLevels.Warning, "Auth.Register", "Credentials empty"));
                return Ok(new APIResult("You must fill the form", APIResultStatus.Failed));
            }
            if (AuthModel.EmailToID.ContainsKey(request.Email!))
            {
                Logger.Log(new Log(LogLevels.Warning, "Auth.Register", "Email already used: " + request.Email));
                return Ok(new APIResult("E-mail is already in use", APIResultStatus.Failed));
            }
            if (AuthModel.UsernameToID.ContainsKey(request.Username!))
            {
                Logger.Log(new Log(LogLevels.Warning, "Auth.Register", "Username already used: " + request.Username));
                return Ok(new APIResult("Username is already in use", APIResultStatus.Failed));
            }
            if (!AuthModel.ValidUsername(request.Username!))
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.Register", "Invalid username: " + request.Username));
                return Ok(new APIResult("Failed", APIResultStatus.Failed, "Username is invalid! (Only letters, numbers and these characters: \"_.\" are allowed)"));
            }
            if (!AuthModel.ValidEmail(request.Email!))
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.Register", "Invalid email: " + request.Email));
                return Ok(new APIResult("Failed", APIResultStatus.Failed, "E-mail is invalid!"));
            }

            Account account = new Account(Guid.NewGuid().ToString(), AuthModel.GenerateToken(), request.Username!, request.Email!, "", request.PasswordHash!, []);
            account.Save();
            Logger.Log(new Log(LogLevels.Information, "Auth.Register", "Successful register: " + account.Token));
            return Ok(new APIResult("Success", APIResultStatus.Success, account.Token!));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.Login", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("get_info")]
    public IActionResult GetInfo([FromBody] LoginRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Token) && AuthModel.TokenToID.TryGetValue(request.Token, out string? token_id))
            {
                Logger.Log(new Log(LogLevels.Information, "Auth.GetInfo", "Fetching info for: " + request.Token));
                Account account = Account.Load(token_id);
                byte[] profile_picture = account.ReadProfilePicture();
                AccountInfo info = new AccountInfo(account.Username!, account.Email!, account.Role!, profile_picture!, account.Tunnels!.Values.ToList());
                return Ok(new APIResult("Success", APIResultStatus.Success, JsonSerializer.Serialize(info)));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.GetInfo", "Token is invalid: " + request.Token));
                return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, ""));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.GetInfo", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("set_info")]
    public IActionResult SetInfo([FromBody] SetAccountInfoRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Request!.Token) && AuthModel.TokenToID.TryGetValue(request.Request.Token, out string? token_id))
            {
                Logger.Log(new Log(LogLevels.Information, "Auth.SetInfo", "Setting info for: " + request.Request.Token));
                Account account = Account.Load(token_id);

                if (account.Email != request.Info!.Email && AuthModel.EmailToID.ContainsKey(request.Info!.Email!))
                {
                    Logger.Log(new Log(LogLevels.Error, "Auth.SetInfo", "Email is already in use: " + request.Info.Email));
                    return Ok(new APIResult("Failed", APIResultStatus.Failed, "Email is already in use"));
                }
                if (account.Username != request.Info!.Username && AuthModel.UsernameToID.ContainsKey(request.Info!.Username!))
                {
                    Logger.Log(new Log(LogLevels.Error, "Auth.SetInfo", "Username is already in use: " + request.Info.Username));
                    return Ok(new APIResult("Failed", APIResultStatus.Failed, "Username is already in use"));
                }
                if (!AuthModel.ValidUsername(request.Info!.Username!))
                {
                    Logger.Log(new Log(LogLevels.Error, "Auth.SetInfo", "Invalid username: " + request.Info.Username));
                    return Ok(new APIResult("Failed", APIResultStatus.Failed, "Username is invalid!"));
                }
                if (!AuthModel.ValidEmail(request.Info!.Email!))
                {
                    Logger.Log(new Log(LogLevels.Error, "Auth.SetInfo", "Invalid email: " + request.Info.Email));
                    return Ok(new APIResult("Failed", APIResultStatus.Failed, "E-mail is invalid!"));
                }

                if (AuthModel.UsernameToID.ContainsKey(account.Username!))
                    AuthModel.UsernameToID.Remove(account.Username!);
                if (AuthModel.EmailToID.ContainsKey(account.Email!))
                    AuthModel.EmailToID.Remove(account.Email!);
                account.Username = request.Info!.Username;
                account.Email = request.Info.Email;
                account.Role = request.Info.Role;
                account.WriteProfilePicture(request.Info.ProfilePicture!);
                account.Save();

                byte[] profile_picture = account.ReadProfilePicture();
                AccountInfo info = new AccountInfo(account.Username!, account.Email!, account.Role!, profile_picture, account.Tunnels!.Values.ToList());
                return Ok(new APIResult("Success", APIResultStatus.Success, JsonSerializer.Serialize(info)));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.SetInfo", "Token is invalid: " + request.Request.Token));
                return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, ""));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.SetInfo", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("delete_account")]
    public IActionResult DeleteAccount([FromBody] LoginRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Token) && AuthModel.TokenToID.TryGetValue(request.Token, out string? token_id))
            {
                Logger.Log(new Log(LogLevels.Information, "Auth.DeleteAccount", "Deleting account: " + request.Token));
                Account account = Account.Load(token_id);
                account.Delete();
                return Ok(new APIResult("Success", APIResultStatus.Success, ""));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.DeleteAccount", "Token is invalid: " + request.Token));
                return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, ""));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.DeleteAccount", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("send_feedback")]
    public IActionResult SendFeedback([FromBody] SendFeedbackRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Request!.Token) && AuthModel.TokenToID.TryGetValue(request.Request!.Token, out string? token_id))
            {
                Logger.Log(new Log(LogLevels.Information, "Auth.SendFeedback", "Feedback sent by: " + request.Request.Token));
                AuthModel.SaveFeedback(token_id, request.Text!);
                return Ok(new APIResult("Success", APIResultStatus.Success, ""));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.SendFeedback", "Token is invalid: " + request.Request.Token));
                return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, ""));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.SendFeedback", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("change_password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Request!.Token) &&
                AuthModel.TokenToID.TryGetValue(request.Request!.Token, out string? token_id))
            {
                Account account = Account.Load(token_id);
                if (BCrypt.Net.BCrypt.Verify(request.Request.PasswordHash, account.PasswordHash))
                {
                    Logger.Log(new Log(LogLevels.Information, "Auth.ChangePassword", "Changing password for: " + request.Request.Token));
                    account.PasswordHash = request.NewPasswordHash;
                    account.Save();
                    return Ok(new APIResult("Success", APIResultStatus.Success, ""));
                }
                else
                {
                    Logger.Log(new Log(LogLevels.Warning, "Auth.ChangePassword", "Invalid current password from: " + request.Request.Token));
                    return Ok(new APIResult("Failed", APIResultStatus.Failed, "Invalid current password!"));
                }
            }
            else if (!string.IsNullOrEmpty(request.Request.Token) &&
                     AuthModel.PasswordResetCodeToID.TryGetValue(request.Request!.Token, out string? code_id))
            {
                AuthModel.PasswordResetCodeToID.Remove(request.Request!.Token);
                Account account = Account.Load(code_id);
                account.PasswordHash = request.NewPasswordHash;
                account.Save();
                return Ok(new APIResult("Success", APIResultStatus.Success, ""));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.ChangePassword", "Token is invalid: " + request.Request.Token));
                return Ok(new APIResult("Token is invalid", APIResultStatus.Failed, ""));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.ChangePassword", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("request_password_reset")]
    public IActionResult RequestPasswordReset([FromBody] RequestPasswordResetRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Email) && AuthModel.EmailToID.TryGetValue(request.Email, out string? email_id))
            {
                AuthModel.PasswordResetCode(email_id, request.Email);
                return Ok(new APIResult("Success", APIResultStatus.Success, ""));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.RequestPasswordReset", "Email is invalid: " + request.Email));
                return Ok(new APIResult("Failed", APIResultStatus.Failed, "E-mail address does not exist"));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.RequestPasswordReset", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }

    [HttpPost("check_password_reset_code")]
    public IActionResult CheckPasswordResetCode([FromBody] CheckPasswordResetCodeRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Code) && AuthModel.PasswordResetCodeToID.TryGetValue(request.Code, out string? code_id))
            {
                Logger.Log(new Log(LogLevels.Information, "Auth.CheckPasswordResetCode", "Successfully checked password reset code for: " + code_id));
                return Ok(new APIResult("Success", APIResultStatus.Success, ""));
            }
            else
            {
                Logger.Log(new Log(LogLevels.Error, "Auth.CheckPasswordResetCode", "Code is invalid: " + request.Code));
                return Ok(new APIResult("Failed", APIResultStatus.Failed, "Code is invalid"));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Auth.CheckPasswordResetCode", ex.Message));
            return Ok(new APIResult("Failed", APIResultStatus.Failed, ex.Message));
        }
    }
}
