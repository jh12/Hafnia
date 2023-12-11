using Hafnia.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Hafnia.Frontend.Components;

public partial class CollectionCard
{
    private string? _styleString;
    private string? _width;
    private string? _height;

    [Parameter]
    public string? Width { set; get; }

    [Parameter]
    public string? Height { set; get; }

    [Parameter]
    public string? Style { set; get; }

    [Parameter]
    public Collection Collection { get; set; } = null!;

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(Width))
        {
            _width = $"width:{Width};";
        }

        if (!string.IsNullOrEmpty(Height))
        {
            _height = $"height:{Height};";
        }

        _styleString = $"{_width}{_height}{Style}";
    }
}
