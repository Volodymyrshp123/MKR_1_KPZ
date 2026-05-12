using System.Text;

namespace LightHtml;

public enum DisplayType
{
    Block,
    Inline
}

public enum ClosingType
{
    WithClosingTag,
    SelfClosing
}

public abstract class LightNode
{
    public abstract string OuterHTML();
    public abstract string InnerHTML();
}

public class LightTextNode : LightNode
{
    private readonly string _text;

    public LightTextNode(string text)
    {
        _text = text;
    }

    public override string OuterHTML() => _text;

    public override string InnerHTML() => _text;
}

public class LightElementNode : LightNode
{
    private readonly List<string> _cssClasses = new();
    private readonly List<LightNode> _children = new();

    public LightElementNode(
        string tagName,
        DisplayType displayType = DisplayType.Block,
        ClosingType closingType = ClosingType.WithClosingTag)
    {
        TagName = tagName;
        DisplayType = displayType;
        ClosingType = closingType;
    }

    public string TagName { get; }
    public DisplayType DisplayType { get; }
    public ClosingType ClosingType { get; }
    public int ChildCount => _children.Count;

    public void AddClass(string className)
    {
        _cssClasses.Add(className);
    }

    public void AddChild(LightNode node)
    {
        if (ClosingType == ClosingType.SelfClosing)
        {
            throw new InvalidOperationException("Self-closing tags cannot contain child nodes.");
        }

        _children.Add(node);
    }

    public override string InnerHTML()
    {
        StringBuilder html = new();

        foreach (LightNode child in _children)
        {
            html.Append(child.OuterHTML());
        }

        return html.ToString();
    }

    public override string OuterHTML()
    {
        string classAttribute = _cssClasses.Count == 0
            ? string.Empty
            : $" class=\"{string.Join(" ", _cssClasses)}\"";

        if (ClosingType == ClosingType.SelfClosing)
        {
            return $"<{TagName}{classAttribute}/>";
        }

        return $"<{TagName}{classAttribute}>{InnerHTML()}</{TagName}>";
    }
}

internal class Program
{
    private static void Main()
    {
        LightElementNode menu = new("ul");
        menu.AddClass("navigation");
        menu.AddClass("main-menu");

        string[] items = { "Home", "Products", "Contacts" };

        foreach (string item in items)
        {
            LightElementNode listItem = new("li");
            LightElementNode link = new("a", DisplayType.Inline);

            link.AddClass("menu-link");
            link.AddChild(new LightTextNode(item));
            listItem.AddChild(link);
            menu.AddChild(listItem);
        }

        Console.WriteLine("=== LightHTML element ===");
        Console.WriteLine(menu.OuterHTML());
        Console.WriteLine();
        Console.WriteLine("=== Element info ===");
        Console.WriteLine($"Tag: {menu.TagName}");
        Console.WriteLine($"Display type: {menu.DisplayType}");
        Console.WriteLine($"Closing type: {menu.ClosingType}");
        Console.WriteLine($"Children: {menu.ChildCount}");
        Console.WriteLine($"InnerHTML: {menu.InnerHTML()}");
    }
}
