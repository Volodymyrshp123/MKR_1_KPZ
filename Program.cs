using System.Text;
using System.Collections.Generic;

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

public enum TraversalType
{
    DepthFirst,
    BreadthFirst
}

// Command Pattern
public interface ICommand
{
    void Execute();
    void Undo();
}

public class AddClassCommand : ICommand
{
    private readonly LightElementNode _element;
    private readonly string _className;

    public AddClassCommand(LightElementNode element, string className)
    {
        _element = element;
        _className = className;
    }

    public void Execute()
    {
        _element.AddClass(_className);
    }

    public void Undo()
    {
        _element.RemoveClass(_className);
    }
}

public class AddChildCommand : ICommand
{
    private readonly LightElementNode _parent;
    private readonly LightNode _child;

    public AddChildCommand(LightElementNode parent, LightNode child)
    {
        _parent = parent;
        _child = child;
    }

    public void Execute()
    {
        _parent.AddChild(_child);
    }

    public void Undo()
    {
        _parent.RemoveChild(_child);
    }
}

public class RemoveChildCommand : ICommand
{
    private readonly LightElementNode _parent;
    private readonly LightNode _child;

    public RemoveChildCommand(LightElementNode parent, LightNode child)
    {
        _parent = parent;
        _child = child;
    }

    public void Execute()
    {
        _parent.RemoveChild(_child);
    }

    public void Undo()
    {
        _parent.AddChild(_child);
    }
}

// State Pattern
public enum ElementStateType
{
    Created,
    Inserted,
    Rendered
}

public interface IElementState
{
    void HandleRender(LightElementNode element);
    ElementStateType StateType { get; }
}

public class CreatedState : IElementState
{
    public ElementStateType StateType => ElementStateType.Created;

    public void HandleRender(LightElementNode element)
    {
        Console.WriteLine($"{element.TagName} is being rendered from Created state");
        element.SetState(new RenderedState());
    }
}

public class InsertedState : IElementState
{
    public ElementStateType StateType => ElementStateType.Inserted;

    public void HandleRender(LightElementNode element)
    {
        Console.WriteLine($"{element.TagName} is being rendered from Inserted state");
        element.SetState(new RenderedState());
    }
}

public class RenderedState : IElementState
{
    public ElementStateType StateType => ElementStateType.Rendered;

    public void HandleRender(LightElementNode element)
    {
        Console.WriteLine($"{element.TagName} is already rendered");
    }
}

// Visitor Pattern
public interface IVisitor
{
    void Visit(LightTextNode node);
    void Visit(LightElementNode node);
}

public class HtmlRendererVisitor : IVisitor
{
    private readonly StringBuilder _html = new();

    public void Visit(LightTextNode node)
    {
        _html.Append(node.OuterHTML());
    }

    public void Visit(LightElementNode node)
    {
        string classAttribute = node.GetCssClasses().Count == 0
            ? string.Empty
            : $" class=\"{string.Join(" ", node.GetCssClasses())}\"";

        if (node.ClosingType == ClosingType.SelfClosing)
        {
            _html.Append($"<{node.TagName}{classAttribute}/>");
        }
        else
        {
            _html.Append($"<{node.TagName}{classAttribute}>");
            foreach (var child in node.GetChildren())
            {
                child.Accept(this);
            }
            _html.Append($"</{node.TagName}>");
        }
    }

    public string GetHtml() => _html.ToString();
}

public class TagCounterVisitor : IVisitor
{
    private readonly Dictionary<string, int> _tagCounts = new();

    public void Visit(LightTextNode node)
    {
        // Text nodes don't count as tags
    }

    public void Visit(LightElementNode node)
    {
        if (_tagCounts.ContainsKey(node.TagName))
        {
            _tagCounts[node.TagName]++;
        }
        else
        {
            _tagCounts[node.TagName] = 1;
        }
        foreach (var child in node.GetChildren())
        {
            child.Accept(this);
        }
    }

    public Dictionary<string, int> GetTagCounts() => _tagCounts;
}

public abstract class LightNode
{
    public abstract string OuterHTML();
    public abstract string InnerHTML();

    // Template Method hooks
    public virtual void OnCreated() { }
    public virtual void OnInserted() { }
    public virtual void OnRemoved() { }
    public virtual void OnStylesApplied() { }
    public virtual void OnClassListApplied() { }
    public virtual void OnTextRendered() { }

    // Iterator
    public abstract IEnumerable<LightNode> GetIterator(TraversalType type);

    // Visitor
    public abstract void Accept(IVisitor visitor);
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

    public override IEnumerable<LightNode> GetIterator(TraversalType type)
    {
        yield return this;
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class LightElementNode : LightNode
{
    private readonly List<string> _cssClasses = new();
    private readonly List<LightNode> _children = new();
    private IElementState _state = new CreatedState();

    public LightElementNode(
        string tagName,
        DisplayType displayType = DisplayType.Block,
        ClosingType closingType = ClosingType.WithClosingTag)
    {
        TagName = tagName;
        DisplayType = displayType;
        ClosingType = closingType;
        OnCreated();
    }

    public string TagName { get; }
    public DisplayType DisplayType { get; }
    public ClosingType ClosingType { get; }
    public int ChildCount => _children.Count;

    public void SetState(IElementState state)
    {
        _state = state;
    }

    public IReadOnlyList<string> GetCssClasses() => _cssClasses.AsReadOnly();
    public IReadOnlyList<LightNode> GetChildren() => _children.AsReadOnly();

    public void AddClass(string className)
    {
        _cssClasses.Add(className);
        OnClassListApplied();
    }

    public void RemoveClass(string className)
    {
        _cssClasses.Remove(className);
        OnClassListApplied();
    }

    public void AddChild(LightNode node)
    {
        if (ClosingType == ClosingType.SelfClosing)
        {
            throw new InvalidOperationException("Self-closing tags cannot contain child nodes.");
        }

        _children.Add(node);
        node.OnInserted();
        SetState(new InsertedState());
    }

    public void RemoveChild(LightNode node)
    {
        if (_children.Remove(node))
        {
            node.OnRemoved();
        }
    }

    public override string InnerHTML()
    {
        StringBuilder html = new();

        foreach (LightNode child in _children)
        {
            html.Append(child.OuterHTML());
        }

        OnTextRendered();
        return html.ToString();
    }

    public override string OuterHTML()
    {
        _state.HandleRender(this);
        string classAttribute = _cssClasses.Count == 0
            ? string.Empty
            : $" class=\"{string.Join(" ", _cssClasses)}\"";

        if (ClosingType == ClosingType.SelfClosing)
        {
            OnStylesApplied();
            return $"<{TagName}{classAttribute}/>";
        }

        OnStylesApplied();
        return $"<{TagName}{classAttribute}>{InnerHTML()}</{TagName}>";
    }

    // Override lifecycle hooks
    public override void OnInserted() { Console.WriteLine($"{TagName} inserted"); }

    public override IEnumerable<LightNode> GetIterator(TraversalType type)
    {
        if (type == TraversalType.DepthFirst)
        {
            yield return this;
            foreach (var child in _children)
            {
                foreach (var node in child.GetIterator(type))
                {
                    yield return node;
                }
            }
        }
        else if (type == TraversalType.BreadthFirst)
        {
            Queue<LightNode> queue = new();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;
                if (current is LightElementNode element)
                {
                    foreach (var child in element._children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
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