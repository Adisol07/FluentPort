using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FluentPort.SDK;

public static class User
{
    public static async Task<APIResult?> Login(LoginRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/login", content);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> Register(LoginRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/register", content);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> GetInfo(LoginRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/get_info", content);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> SetInfo(SetAccountInfoRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/set_info", content);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> DeleteAccount(LoginRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/delete_account", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> SendFeedback(SendFeedbackRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/send_feedback", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/change_password", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> RequestPasswordResetRequest(RequestPasswordResetRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/request_password_reset", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    public static async Task<APIResult?> CheckPasswordResetCode(CheckPasswordResetCodeRequest request)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(request);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/auth/check_password_reset_code", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
}
