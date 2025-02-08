using System.Text;
using Microsoft.EntityFrameworkCore;
using Personalblog.Extensions.SendEmail;
using Personalblog.Extensions.SendEmail.Services;
using Personalblog.Migrate;
using Personalblog.Model;
using Personalblog.Model.Entitys;
using Personalblog.Model.ViewModels;
using Personalblog.Model.ViewModels.Categories;
using Personalblog.Model.ViewModels.MessageBoard;
using Personalblog.Model.ViewModels.QueryFilters;
using PersonalblogServices.Response;
using Msg = Personalblog.Model.Entitys.Messages;
using X.PagedList;

namespace PersonalblogServices.Messages;

public class MessagesService:IMessagesService
{
    private readonly MyDbContext _myDbContext;
    private IEmailService emailService;
    private readonly EmailServiceFactory _emailServiceFactory;
    public MessagesService(MyDbContext myDbContext,EmailServiceFactory emailServiceFactory)
    {
        _myDbContext = myDbContext;
        _emailServiceFactory = emailServiceFactory;
    }
    public async Task<Personalblog.Model.Entitys.Messages> SubmitMessageAsync(Personalblog.Model.Entitys.Messages messages)
    {
        StringBuilder sb = CommentSJson.CommentsJson(messages.Message);
        messages.Message = sb.ToString();
        messages.created_at = DateTime.Now;
        await _myDbContext.Messages.AddAsync(messages);
        await _myDbContext.SaveChangesAsync();
        return messages;
    }

    public IPagedList<Personalblog.Model.Entitys.Messages> GetAll(QueryParameters param)
    {
        return _myDbContext.Messages.Include(m => m.Replies).ToList().ToPagedList(param.Page, param.PageSize);
        // return _myDbContext.Messages.ToList().ToPagedList(param.Page, param.PageSize);
    }

    public async Task<List<Personalblog.Model.Entitys.Messages>> GetAllasync()
    {
        return await _myDbContext.Messages.ToListAsync();
    }

    public async Task<ApiResponse> DelMessageAsync(int id)
    {
        try
        {
            using (var transaction = await _myDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var msg = new Msg() { Id = id };
                    _myDbContext.Messages.Attach(msg);
                    _myDbContext.Messages.Remove(msg);

                    var replies = await _myDbContext.Replies.Where(r => r.MessageId == id).ToListAsync();
                    _myDbContext.Replies.RemoveRange(replies);

                    await _myDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new ApiResponse { Message = $"留言{id}删除成功" };
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(e);
                    return new ApiResponse { Message = "删除失败！", StatusCode = 500 };
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new ApiResponse { Message = "删除失败！", StatusCode = 500 };
        }
    }

    public async Task<Replies> ReplyMessageAsync(Replies replies)
    {
        StringBuilder sb = CommentSJson.CommentsJson(replies.Reply);
        replies.Reply = sb.ToString();
        replies.created_at = DateTime.Now;
        await _myDbContext.Replies.AddAsync(replies);
        await _myDbContext.SaveChangesAsync();
        return replies;
    }

    public async Task<List<Replies>> GetMsgReplyAsync()
    {
        return await _myDbContext.Replies.ToListAsync();
    }

    public IPagedList<Replies> GetAllReply(QueryParameters param)
    {
        return _myDbContext.Replies.ToList().ToPagedList(param.Page, param.PageSize);
    }

    public async Task<ApiResponse> DelMessageReplyAsync(int id)
    {
        try
        {
            var replies = new Replies(){Id = id};
            _myDbContext.Replies.Attach(replies);
            _myDbContext.Replies.Remove(replies);
            await _myDbContext.SaveChangesAsync();
            return new ApiResponse { Message = $"留言{id}删除成功"};
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new ApiResponse { Message = "删除失败！", StatusCode = 500 };
        }
    }

    public async Task SendEmailOnAdd(string email, string content)
    {
        emailService = await _emailServiceFactory.CreateEmailService();
        var template = new MessageBoardNotificationEmailTemplate();
        await emailService.SendEmail(email, template,
            new EmailContent() { Content = content });
    }

    public async Task<(List<MessageBoardList>, PaginationMetadata)> GetPageList(MsgBoardQueryParameter param)
    {
        // 获取所有留言以及回复，并将其展平  
        var messages = _myDbContext.Messages  
            .Include(m => m.Replies)  
            .Select(m => new MessageBoardList  
            {  
                Id = m.Id,  
                Name = m.Name,  
                Email = m.Email,  
                Message = m.Message,  
                CreatedAt = m.created_at,  
                ReplyToName = null, // 留言的回复人为空  
                IsReply = false // 标识为留言  
            })  
            .ToList();  

        var replies = _myDbContext.Replies  
            .Select(r => new MessageBoardList  
            {  
                Id = r.Id,  
                Name = r.Name,  
                Email = r.Email,  
                Message = r.Reply,  
                CreatedAt = r.created_at,  
                ReplyToName = r.Name, // 回复的回复人是回复者的名称  
                IsReply = true // 标识为回复  
            })  
            .ToList();  

        // 将留言和回复合并  
        var result = messages
            .Concat(replies)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(m => m.IsReply) // 留言在前，回复在后
            .ToList();
        
        var data = await result
            .Skip((param.Page - 1) * param.PageSize)
            .Take(param.PageSize)
            .ToListAsync();

        var pagination = new PaginationMetadata()
        {
            PageNumber = param.Page,
            PageSize = param.PageSize,
            TotalItemCount = result.Count
        };
        return (data, pagination);
    }
}