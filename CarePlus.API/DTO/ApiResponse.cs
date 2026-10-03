
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public int RespCode { get; set; }
    public string RespMessage { get; set; } = string.Empty;
    public T? Data { get; set; }


    public ApiResponse() { }


    public ApiResponse(bool isSuccess, int respCode, string respMessage, T? data = default)
    {
        IsSuccess = isSuccess;
        RespCode = respCode;
        RespMessage = respMessage;
        Data = data;
    }

    // Static Factory Methods
    public static ApiResponse<T> Ok(T? data, string message = "Success")
    {
        return new ApiResponse<T>(true, 200, message, data);
    }

    public static ApiResponse<T> Fail(string message, int code = 999, T? data = default)
    {
        return new ApiResponse<T>(false, code, message, data);
    }
}