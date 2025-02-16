using System.Diagnostics;
using System.Text;
using FluentAssertions;
using MarkdownProcessor;
using MarkdownProcessor.Renderer;
using MarkdownProcessor.Tags;
using NUnit.Framework;

namespace MarkdownProcessorTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class Md_Should
{
    private readonly IRenderer renderer = new HtmlRenderer(new Dictionary<TextType, string>
    {
        { TextType.Italic, "em" },
        { TextType.Bold, "strong" },
        { TextType.FirstHeader, "h1" }
    });

    private readonly Md md = new(new ITagMarkdownConfig[]
    {
        new ItalicConfig(),
        new BoldConfig(),
        new FirstHeaderConfig()
    });

    [TestCase("_word word_", "<em>word word</em>", TestName = "Single underline to italic text")]
    [TestCase("__word word__", "<strong>word word</strong>", TestName = "Double underline to bold text")]
    [TestCase("_word word", "_word word", TestName = "Unclosed italic tag dont work")]
    [TestCase("__word word", "__word word", TestName = "Unclosed bold tag dont work")]
    [TestCase("_word word__", "_word word__", TestName = "Double underline dont close single")]
    [TestCase("__word _word_ word__", "<strong>word <em>word</em> word</strong>",
        TestName = "Italic tag work in bold tag")]
    [TestCase("_word __word__ word_", "<em>word __word__ word</em>",
        TestName = "Bold tag don't work in italic tag")]
    [TestCase("w_ord_", "w<em>ord</em>", TestName = "Italic tag work inside word")]
    [TestCase("w__ord__", "w<strong>ord</strong>", TestName = "Bold tag work inside word")]
    [TestCase("w_or_d", "w<em>or</em>d", TestName = "Italic tag work on word part")]
    [TestCase("w__or__d", "w<strong>or</strong>d", TestName = "Bold tag work on word part")]
    [TestCase("word1_2_", "word1_2_", TestName = "Tag don't work between digits")]
    [TestCase("word_12_", "word<em>12</em>", TestName = "Tag work over digits")]
    [TestCase("w_ord wor_d", "w_ord wor_d", TestName = "Italic tag don't work inside different words")]
    [TestCase("w__ord wor__d", "w__ord wor__d", TestName = "Bold tag don't work inside different words")]
    [TestCase("_word\nword_", "_word\nword_", TestName = "Italic tag don't work on different paragraphs")]
    [TestCase("__word\nword__", "__word\nword__", TestName = "Bold tag don't work on different paragraphs")]
    [TestCase("_ word_", "_ word_", TestName = "After opening tag should be a non-blank symbol")]
    [TestCase("_word _", "_word _", TestName = "Before closing tag should be a non-blank symbol")]
    [TestCase("__word _word__ word_", "__word _word__ word_", TestName = "Intersecting tags don't work")]
    [TestCase("____", "____", TestName = "Tags dont work on blank string")]
    public void Render_ReturnCorrectHTMLText_OnBoldAndItalic(string text, string expected)
    {
        var html = md.Render(text, renderer);

        html.Should().Be(expected);
    }

    [TestCase(@"\_word word_", "_word word_", TestName = "Backslash shield symbol")]
    [TestCase(@"_word word\__", "<em>word word_</em>", TestName = "Backslash shield only one symbol")]
    [TestCase(@"\word word", @"\word word", TestName = "Backslash not shield not special symbol")]
    [TestCase(@"__word word\\__", @"<strong>word word\</strong>", TestName = "Backslash shield backslash")]
    public void Render_ReturnCorrectHTMLText_OnBackslash(string text, string expected)
    {
        var html = md.Render(text, renderer);

        html.Should().Be(expected);
    }

    [TestCase("# header", "<h1>header</h1>", TestName = "Hashtag with space start header")]
    [TestCase("#header", "#header", TestName = "Hashtag with no space dont start header")]
    [TestCase("# header\nword", "<h1>header</h1>word", TestName = "Escape stop header")]
    [TestCase("# hea# der", "<h1>hea# der</h1>", TestName = "Hashtag work inside header")]
    [TestCase("# header _header_", "<h1>header <em>header</em></h1>", TestName = "Italic tag work inside header")]
    [TestCase("# header __header__", "<h1>header <strong>header</strong></h1>",
        TestName = "Bold tag work inside header")]
    [TestCase("not # header", "not # header", TestName = "Header only on paragraph start")]
    public void Render_ReturnCorrectHTMLText_OnHeader(string text, string expected)
    {
        var html = md.Render(text, renderer);

        html.Should().Be(expected);
    }

    [Test]
    public void Render_IsLinearAlgorithm()
    {
        const string line = "# header with _italic_ and __bold__";
        var manyLines = new StringBuilder();
        for (var i = 0; i < 10000; i++) manyLines.AppendLine(line);
        var times = new int[9];

        var sb = new StringBuilder(manyLines.ToString(), 100000);
        _ = md.Render(sb.ToString(), renderer);
        var sw = new Stopwatch();
        for (var i = 0; i < 9; i++)
        {
            sb.Append(manyLines);
            times[i] = MakeMeasurement(sb.ToString(), sw);
        }

        GetIncreases(times).Should().AllSatisfy(d => d.Should().BeApproximately(0, 0.25));
    }

    private int MakeMeasurement(string text, Stopwatch sw)
    {
        sw.Reset();
        sw.Start();
        _ = md.Render(text, renderer);
        sw.Stop();
        return (int)sw.ElapsedMilliseconds;
    }

    private static IEnumerable<double> GetIncreases(int[] times)
    {
        var increases = times
            .Skip(1).Select((t, i) => (double)times[i] / t).ToArray();
        return increases.Select(i => i - increases[0]);
    }
}