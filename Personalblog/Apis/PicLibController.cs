using Microsoft.AspNetCore.Mvc;
using Personalblog.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Personalblog.Apis
{
    public class PicLibController : Controller
    {
        private readonly PiCLibService _service;
        private readonly IConfiguration _configuration;
        private readonly QiniuService _qiniuService;
        public static string FontFamily = "Arial";
        public PicLibController(PiCLibService service,IConfiguration configuration,
            QiniuService qiniuService)
        {
            _service = service;
            _configuration = configuration;
            _qiniuService = qiniuService;
            FontFamily = _configuration.GetValue<string>("FontFamily");
        }
        private static async Task<IActionResult> GenerateImageResponse(Image image, IImageFormat format)
        {
            var encoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(format);
            using (var stream = new MemoryStream())
            {
                //windows字体 Arial
                //liunx字体 DejaVu Sans
                float fontSize = image.Width / 15f; // 根据实际需要调整比例
                var font = SystemFonts.CreateFont(FontFamily, fontSize);
                var textSize = TextMeasurer.Measure("ZY blog", new TextOptions(font));
                // 计算水印显示位置，在图片右下角
                var location = new PointF(
                    image.Width - textSize.Width - 10, // 10 为右边距
                    image.Height - textSize.Height - 10 // 10 为下边距
                );
                image.Mutate(ctx => ctx.DrawText("ZY blog", font, new Rgba32(255, 255, 255, 128), location));
                
                await image.SaveAsync(stream, encoder);
                try
                {
                    return new FileContentResult(stream.ToArray(), "image/jpeg");
                }
                finally
                {
                    image.Dispose();
                }
            }
        }
        /// <summary>
        /// 指定尺寸随机图片
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns></returns>
        [HttpGet("Random/{width:int}/{height:int}")]
        public async Task<IActionResult> GetRandomImage(int width,int height)
        {
            var (image, format) = await _service.GetRandomImageAsync(width, height);
            try
            {
                return await GenerateImageResponse(image, format);
            }
            finally
            {
                image.Dispose();
            }
        }
        [HttpGet("RandomTop/{seed}/")]
        public async Task<IActionResult> GetRandomImageTop(string seed)
        {
            var (image,format) = await _service.GetRandomImageAsyncTop(seed);
            try
            {
                return await GenerateImageResponse(image, format);
            }
            finally
            {
                image.Dispose();
            }
        }

        [HttpGet]
        public async Task<List<string>> GetRandomImageTopQiliu(string? seed)
        {
            // string path = await _service.GetQiliuImageAsyncTop();
            // string path = await _qiniuService.GetRandomImageAsync();
            return await _service.GetQiliuImageAsyncTop();
        }
        /// <summary>
        /// 指定尺寸随机图片 (带初始种子)
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        [HttpGet("Random/{seed}/{width:int}/{height:int}")]
        public async Task<IActionResult> GetRandomImage(string seed, int width, int height)
        {
            var (image, format) = await _service.GetRandomImageAsync(width, height, seed);
            try
            {
                return await GenerateImageResponse(image, format);
            }
            finally
            {
                image.Dispose();
            }
        }
        /// <summary>
        /// 返回指定图片
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns></returns>
        [HttpGet("Design/{width:int}/{height:int}")]
        public async Task<IActionResult> GetDesignImage(string seed,int width,int height)
        {
            var (image, format) = await _service.GetDesignImageAsync(width, height, seed);
            try
            {
                return await GenerateImageResponse(image, format);
            }
            finally
            {
                image.Dispose();
            }
        }
    }
}
