using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace WebScraper.Core
{
    public class HtmlContentParser
    {
        private IDocument _dom;

        public HtmlContentParser(string body)
        {
            var ctx = BrowsingContext.New(Configuration.Default);
            var parser = ctx.GetService<IHtmlParser>();
            _dom = parser.ParseDocument(body);
        }


        public IHtmlCollection<IElement> GetFirstChild(string selector)
        {
            return _dom.QuerySelectorAll(selector);
        }
    }
}
