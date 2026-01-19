namespace Shalimar.Razor;

/// <summary>
/// Transforms HTML/Razor attributes to JSX-compatible attributes.
///
/// Key transformations:
/// - class → className (React requirement)
/// - for → htmlFor (React requirement)
/// - @onclick → onClick (Blazor events to React events)
/// - tabindex → tabIndex (JSX camelCase)
/// - Various SVG attributes
/// </summary>
public static class AttributeTransformer
{
    /// <summary>
    /// Maps HTML attribute names to JSX attribute names.
    /// </summary>
    private static readonly Dictionary<string, string> AttributeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // HTML to JSX renames
        { "class", "className" },
        { "for", "htmlFor" },
        { "tabindex", "tabIndex" },
        { "readonly", "readOnly" },
        { "maxlength", "maxLength" },
        { "minlength", "minLength" },
        { "colspan", "colSpan" },
        { "rowspan", "rowSpan" },
        { "cellpadding", "cellPadding" },
        { "cellspacing", "cellSpacing" },
        { "usemap", "useMap" },
        { "frameborder", "frameBorder" },
        { "contenteditable", "contentEditable" },
        { "crossorigin", "crossOrigin" },
        { "datetime", "dateTime" },
        { "enctype", "encType" },
        { "formaction", "formAction" },
        { "formenctype", "formEncType" },
        { "formmethod", "formMethod" },
        { "formnovalidate", "formNoValidate" },
        { "formtarget", "formTarget" },
        { "hreflang", "hrefLang" },
        { "inputmode", "inputMode" },
        { "srcdoc", "srcDoc" },
        { "srcset", "srcSet" },
        { "autofocus", "autoFocus" },
        { "autoplay", "autoPlay" },
        { "autocomplete", "autoComplete" },
        { "charset", "charSet" },
        { "allowfullscreen", "allowFullScreen" },

        // Blazor events to React events
        { "@onclick", "onClick" },
        { "@ondblclick", "onDoubleClick" },
        { "@onchange", "onChange" },
        { "@oninput", "onInput" },
        { "@onsubmit", "onSubmit" },
        { "@onkeydown", "onKeyDown" },
        { "@onkeyup", "onKeyUp" },
        { "@onkeypress", "onKeyPress" },
        { "@onmousedown", "onMouseDown" },
        { "@onmouseup", "onMouseUp" },
        { "@onmousemove", "onMouseMove" },
        { "@onmouseenter", "onMouseEnter" },
        { "@onmouseleave", "onMouseLeave" },
        { "@onmouseover", "onMouseOver" },
        { "@onmouseout", "onMouseOut" },
        { "@onfocus", "onFocus" },
        { "@onblur", "onBlur" },
        { "@onscroll", "onScroll" },
        { "@ondrag", "onDrag" },
        { "@ondragstart", "onDragStart" },
        { "@ondragend", "onDragEnd" },
        { "@ondragenter", "onDragEnter" },
        { "@ondragleave", "onDragLeave" },
        { "@ondragover", "onDragOver" },
        { "@ondrop", "onDrop" },
        { "@ontouchstart", "onTouchStart" },
        { "@ontouchend", "onTouchEnd" },
        { "@ontouchmove", "onTouchMove" },
        { "@ontouchcancel", "onTouchCancel" },

        // SVG attributes
        { "viewbox", "viewBox" },
        { "preserveaspectratio", "preserveAspectRatio" },
        { "xlink:href", "xlinkHref" },
        { "xmlns:xlink", "xmlnsXlink" },
        { "fill-opacity", "fillOpacity" },
        { "stroke-width", "strokeWidth" },
        { "stroke-linecap", "strokeLinecap" },
        { "stroke-linejoin", "strokeLinejoin" },
        { "stroke-dasharray", "strokeDasharray" },
        { "stroke-dashoffset", "strokeDashoffset" },
        { "stroke-opacity", "strokeOpacity" },
        { "font-family", "fontFamily" },
        { "font-size", "fontSize" },
        { "text-anchor", "textAnchor" },
        { "clip-path", "clipPath" },
        { "fill-rule", "fillRule" },
    };

    /// <summary>
    /// Transform an HTML/Razor attribute name to its JSX equivalent.
    /// </summary>
    public static string TransformName(string htmlAttribute)
    {
        if (string.IsNullOrEmpty(htmlAttribute))
        {
            return htmlAttribute;
        }

        // Check direct mapping first
        if (AttributeMap.TryGetValue(htmlAttribute, out var jsxAttribute))
        {
            return jsxAttribute;
        }

        // Handle data-* and aria-* attributes (keep as-is)
        if (htmlAttribute.StartsWith("data-", StringComparison.OrdinalIgnoreCase) ||
            htmlAttribute.StartsWith("aria-", StringComparison.OrdinalIgnoreCase))
        {
            return htmlAttribute;
        }

        // Handle @bind specially
        if (htmlAttribute.Equals("@bind", StringComparison.OrdinalIgnoreCase))
        {
            return "value"; // Note: onChange handler needs to be added separately
        }

        // Handle @bind:event
        if (htmlAttribute.StartsWith("@bind:", StringComparison.OrdinalIgnoreCase))
        {
            return "value";
        }

        // Handle generic @on* events (remove @ and convert to camelCase)
        if (htmlAttribute.StartsWith("@on", StringComparison.OrdinalIgnoreCase))
        {
            var eventName = htmlAttribute.Substring(3); // Remove "@on"
            return "on" + char.ToUpper(eventName[0]) + eventName.Substring(1);
        }

        // Return original if no transformation needed
        return htmlAttribute;
    }

    /// <summary>
    /// Transform an attribute value, handling expression binding.
    /// </summary>
    public static (string name, string value, bool isExpression) TransformAttribute(
        string name,
        string value)
    {
        var jsxName = TransformName(name);
        var isExpression = false;
        var jsxValue = value;

        // Handle @bind value transformation
        if (name.Equals("@bind", StringComparison.OrdinalIgnoreCase))
        {
            isExpression = true;
            jsxValue = ExpressionTransformer.Transform(value.TrimStart('@'));
        }
        // Handle @ prefix in value (C# expression)
        else if (value.StartsWith("@"))
        {
            isExpression = true;
            jsxValue = ExpressionTransformer.Transform(value.TrimStart('@'));
        }
        // Handle event handler with lambda
        else if (name.StartsWith("@on") && value.Contains("=>"))
        {
            isExpression = true;
            jsxValue = TransformLambdaHandler(value);
        }

        return (jsxName, jsxValue, isExpression);
    }

    /// <summary>
    /// Transform a Blazor lambda event handler to React format.
    /// </summary>
    private static string TransformLambdaHandler(string value)
    {
        // Remove @ prefix and parentheses wrapper if present
        value = value.Trim();
        if (value.StartsWith("@"))
        {
            value = value.Substring(1);
        }
        if (value.StartsWith("(") && value.EndsWith(")"))
        {
            value = value.Substring(1, value.Length - 2);
        }

        // Transform the lambda expression
        return ExpressionTransformer.Transform(value);
    }
}
