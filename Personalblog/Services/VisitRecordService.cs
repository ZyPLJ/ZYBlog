using Microsoft.EntityFrameworkCore;
using Personalblog.Model;
using Personalblog.Model.Entitys;
using Personalblog.Model.ViewModels.QueryFilters;
using Personalblog.Model.ViewModels.Categories;

namespace Personalblog.Services
{
    public class VisitRecordService
    {
        private readonly MyDbContext _myDbContext;
        public VisitRecordService(MyDbContext myDbContext)
        {
            _myDbContext = myDbContext;
        }
        /// <summary>
        /// 总览数据
        /// </summary>
        /// <returns></returns>
        public object Overview()
        {
            return _myDbContext.visitRecords.
                Where(a => !a.RequestPath.StartsWith("/Api")).
                GroupBy(a => new {}).
                Select(g => new
                {
                    TotalVisit = g.Count(),
                    TodayVisit = g.Sum(g=>g.Time.Date == DateTime.Today ? 1 : 0),
                    YesterdayVisit = g.Sum(g=>g.Time.Date == DateTime.Today.AddDays(-1).Date ? 1 : 0)
                });
        }

        /// <summary>
        /// 趋势数据
        /// </summary>
        /// <param name="days">查看最近几天的数据，默认7天</param>
        /// <returns></returns>
        public object Trend(int days = 7)
        {
            return _myDbContext.visitRecords.
                Where(a => !a.RequestPath.StartsWith("/Api")).
                GroupBy(a => a.Time.Date).
                Select(a => new
                {
                    time = a.Key,
                    date = $"{a.Key.Month}-{a.Key.Day}",
                    count = a.Count()
                }).ToList();
        }
        public List<VisitRecord> GetAll()
        {
            return _myDbContext.visitRecords.OrderByDescending(a => a.Time).ToList();
        }
        public async Task<(List<VisitRecord>, PaginationMetadata)>GetPagedList(VisitRecordQueryParameters param)
        {
            var querySet = _myDbContext.visitRecords.AsQueryable();
            // 搜索
            if (!string.IsNullOrEmpty(param.Search))
            {
                querySet = querySet.Where(a => a.RequestPath.Contains(param.Search));
            }

            var data = await querySet
                .OrderByDescending(a => a.Id)
                .Skip((param.Page - 1) * param.PageSize)
                .Take(param.PageSize)
                .ToListAsync();
            
            var pagination = new PaginationMetadata()
            {
                PageNumber = param.Page,
                PageSize = param.PageSize,
                TotalItemCount = await querySet.CountAsync()
            };
            
            return (data, pagination);
        }

        public async Task<object> GetVisitRecoredNew()
        {
            var now = DateTime.Now;
            var sevenDaysAgo = now.AddDays(-7);

            var totalPosts = await _myDbContext.posts.CountAsync();
            var totalReads = await _myDbContext.posts.SumAsync(p => p.ViewCount);
            var totalComments = await _myDbContext.Comments.CountAsync();
            var totalVisitRecords = await _myDbContext.visitRecords.CountAsync();

            var accessPosts = await _myDbContext.posts.Where(ap => ap.CreationTime >= sevenDaysAgo)
                .GroupBy(ap => ap.CreationTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();
            var postsData = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i).Date) // 生成过去七天的日期
                .Select(date => accessPosts.FirstOrDefault(c => c.Date == date)?.Count ?? 0) // 查找评论数量并填充0
                .ToList();
            var postsPercent = totalPosts > 0 ? (postsData.Sum(g => g) * 100.0 / totalPosts).ToString("F2") + "%" : "0%";

            var accessReads = await _myDbContext.posts.Where(ar => ar.ViewCount > 0 && ar.CreationTime >= sevenDaysAgo && ar.CreationTime != now).CountAsync();
            var readsPercent = totalReads > 0 ? (accessReads * 100.0 / totalReads).ToString("F2") + "%" : "0%";

            var accessComments = await _myDbContext.Comments.Where(ac => ac.CreatedTime >= sevenDaysAgo)
                .GroupBy(ac => ac.CreatedTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();
            var commentsData = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i).Date) // 生成过去七天的日期
                .Select(date => accessComments.FirstOrDefault(c => c.Date == date)?.Count ?? 0) // 查找评论数量并填充0
                .ToList();
            var commentsPercent = totalComments > 0
                ? (commentsData.Sum(g => g) * 100.0 / totalComments).ToString("F2") + "%"
                : "0%";

            var accessVisitRecords = await _myDbContext.visitRecords.Where(av => av.Time >= sevenDaysAgo)
                .GroupBy(av => av.Time.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();
            var visitRecordsData = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i).Date) // 生成过去七天的日期
                .Select(date => accessVisitRecords.FirstOrDefault(c => c.Date == date)?.Count ?? 0) // 查找评论数量并填充0
                .ToList();
            var visitRecordsPercent = totalVisitRecords > 0
                ? (visitRecordsData.Sum(g => g) * 100.0 / totalVisitRecords).ToString("F2") + "%"
                : "0%";

            return new[]
            {
                new
                {
                    name = "访问总量", value = totalVisitRecords, percent = visitRecordsPercent, data = visitRecordsData
                },
                new { name = "阅读总量", value = totalReads, percent = readsPercent, data = new List<int>{accessReads} },
                new { name = "文章数量", value = totalPosts, percent = postsPercent, data = postsData },
                new { name = "评论数量", value = totalComments, percent = commentsPercent, data = commentsData },
            };

        }

        private static DateTime StartOfWeek(DateTime dateTime, DayOfWeek startOfWeek)  
        {  
            int diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;  
            return dateTime.AddDays(-1 * diff).Date;  
        }  
        public async Task<object> GetChatDate()
        {
            var now = DateTime.Now;
            var startOfWeek = StartOfWeek(now,DayOfWeek.Monday); // 获取本周的开始日期  
            var startOfLastWeek = startOfWeek.AddDays(-7); // 上周开始日期  

            // 查询本周的访问数量
            var thisWeekVisits = await _myDbContext.visitRecords
                .Where(v => v.Time >= startOfWeek && v.Time < startOfWeek.AddDays(7))
                .GroupBy(v => v.Time.Date) // 按日期分组  
                .Select(g => new { Date = g.Key, Count = g.Count() }) 
                .ToListAsync();
            
            // 确保返回的数据填充为 7 天，不足的用 0 填充  
            var thisWeekVisitsData = Enumerable.Range(0, 7)
                .Select(i => startOfWeek.AddDays(i).Date) // 根据 startOfWeek 生成这一周的日期
                .Select(date => thisWeekVisits.FirstOrDefault(v => v.Date == date)?.Count ?? 0) // 根据日期查找访问数量，找不到则为0
                .ToList();
            
            // 查询上周的访问数量
            var lastWeekVisits = await _myDbContext.visitRecords
                .Where(v => v.Time >= startOfLastWeek && v.Time < startOfLastWeek.AddDays(7))
                .GroupBy(v => v.Time.Date) // 按日期分组  
                .Select(g => new { Date = g.Key, Count = g.Count() }) 
                .ToListAsync();
            

            // 确保返回的数据填充为 7 天，不足的用 0 填充  
            var lastWeekVisitsData = Enumerable.Range(0, 7)
                .Select(i => startOfLastWeek.AddDays(i).Date) // 根据 startOfWeek 生成这一周的日期
                .Select(date => lastWeekVisits.FirstOrDefault(v => v.Date == date)?.Count ?? 0) // 根据日期查找访问数量，找不到则为0
                .ToList();

            // 格式化返回结果  
            var result = new[]
            {
                new
                {
                    requireData = lastWeekVisitsData
                },
                new
                {
                    requireData = thisWeekVisitsData
                }
            };

        // 返回结果  
            return result;
        }
    }
}
