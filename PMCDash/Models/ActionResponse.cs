using NJsonSchema.Annotations;
namespace PMCDash.Models
{
    public class ActionResponse<T>
    {
        /// <summary>
        /// # http-200
        /// 200: 有時候是"N/A"也可以說沒消息就空白?
        /// # http-40x
        /// 40x: 基本上都會有錯誤訊息的拉~
        /// </summary>
        public string Message { get; set; } = "N/A";

        /// <summary>
        /// # http-200
        /// 200: 有回傳資料就看是什麼樣的內容
        /// # http-40x
        /// 40x: 基本上會沒有Data指定為null ? 
        /// </summary>

        [NotNull]
        public T Data { get; set; }
    }
}
