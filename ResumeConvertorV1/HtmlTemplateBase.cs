using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace ResumeConvertorV1
{
    /// <summary>
    /// RazorEngine template base class for HTML rendering with raw HTML support.
    /// 
    /// This abstract class extends <see cref="TemplateBase{T}"/> and provides a helper method <see cref="Raw(object)"/>
    /// to safely inject raw HTML content into Razor templates without encoding.
    /// 
    /// Usage:
    /// - In your Razor HTML template, call @Raw(variable) to render unencoded HTML, e.g., to output rich content.
    /// - This class is set as the BaseTemplateType in RazorEngine configuration, so all templates can use this helper.
    /// </summary>
    /// <typeparam name="T">The model type used by the template.</typeparam>

    public abstract class HtmlTemplateBase<T> : TemplateBase<T>
    {
        /// <summary>
        /// Returns the input string as a raw, unencoded HTML string, suitable for rendering HTML content directly.
        /// If the input is null, returns an empty string.
        /// </summary>
        /// <param name="rawString">The object or string to render as raw HTML.</param>
        /// <returns>An <see cref="IEncodedString"/> that RazorEngine will not encode.</returns>
        public IEncodedString Raw(object rawString)
        {
            return new RawString(rawString?.ToString() ?? "");
        }
    }

}
