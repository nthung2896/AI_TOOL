namespace Hinet.Service.MoitSsoService.Dto
{
    // Response chuẩn từ Cổng DVC: { message, note, data }
    public class MoitResponse<T>
    {
        public int Message { get; set; }
        public string? Note { get; set; }
        public T? Data { get; set; }

        public bool IsSuccess => Message == 0;
    }
}
