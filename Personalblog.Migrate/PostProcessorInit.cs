using System.Web;
using Markdig;
using Markdig.Extensions.MediaLinks;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Personalblog.Model.Entitys;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Personalblog.Migrate;

public class PostProcessorInit
{
    private readonly Post _post;
    private readonly string _importPath;
    private readonly string _assetsPath;

    public PostProcessorInit(string importPath, string assetsPath, Post post)
    {
        _post = post;
        _assetsPath = assetsPath;
        _importPath = importPath;
    }

    /// <summary>
    /// Markdown内容解析，复制图片 & 替换图片链接
    /// </summary>
    /// <returns></returns>
    public string MarkdownParse() {
        if (_post.Content == null) {
            return string.Empty;
        }

        var document = Markdown.Parse(_post.Content);

        foreach (var node in document.AsEnumerable()) {
            if (node is not ParagraphBlock { Inline: { } } paragraphBlock) continue;
            foreach (var inline in paragraphBlock.Inline) {
                if (inline is not LinkInline { IsImage: true } linkInline) continue;

                var imgUrl = HttpUtility.UrlDecode(linkInline.Url);
                if (imgUrl == null) continue;
                if (imgUrl.StartsWith("http")) continue;

                // 路径处理
                var imgPath = Path.Combine(_importPath, _post.Path ?? "", imgUrl);
                var imgFilename = Path.GetFileName(imgUrl);
                var destDir = Path.Combine(_assetsPath, _post.Id);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                var destPath = Path.Combine(destDir, imgFilename);
                if (File.Exists(destPath)) {
                    // 图片重名处理
                    var imgId = GuidUtils.GuidTo16String();
                    imgFilename =
                        $"{Path.GetFileNameWithoutExtension(imgFilename)}-{imgId}.{Path.GetExtension(imgFilename)}";
                    destPath = Path.Combine(destDir, imgFilename);
                }

                // 替换图片链接
                linkInline.Url = Path.Combine("/media/blog/", _post.Id, imgFilename);
                // 复制图片
                File.Copy(imgPath, destPath);

                Console.WriteLine($"复制 {imgPath} 到 {destPath}");
            }
        }


        using var writer = new StringWriter();
        var render = new NormalizeRenderer(writer);
        render.Render(document);
        return writer.ToString();
    }

    /// <summary>
    /// 从正文提取前length字的概括
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public string GetSummary(int length)
    {
        return _post.Content == null
            ? string.Empty
            : Markdown.ToPlainText(_post.Content).Limit(length);
    }

    /// <summary>
    /// 图片压缩
    /// </summary>
    /// <param name="imagePath">原路径</param>
    /// <param name="outputPath">新路径</param>
    /// <param name="quality">压缩质量 1-100 数值越低压缩越多</param>
    public void CompressImage(string imagePath, string outputPath, int quality)
    {
        //如果是gif图片，则不压缩
        if (imagePath.EndsWith(".gif"))
        {
            File.Copy(imagePath, outputPath);
            return;
        }

        using var image = Image.Load(imagePath);
        var encoder = new JpegEncoder()
        {
            Quality = quality // 设置压缩质量
        };
        image.Save(outputPath, encoder);
    }
}