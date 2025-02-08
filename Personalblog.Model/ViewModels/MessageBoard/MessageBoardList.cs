using Personalblog.Model.Entitys;

namespace Personalblog.Model.ViewModels.MessageBoard;

public class MessageBoardList
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReply { get; set; } // 是否为回复
    public string ReplyToName { get; set; }
}