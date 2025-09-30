using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using AWUI.Enums;
using AWUI.Events;
using AWUI.Helper;

namespace AWUI.Components;

public class Input<TValue> : AWComponentBase
{
    protected ElementReference inputElement;

    protected TValue? currentValue;

    [Parameter]
    public InputType Type { get; set; } = InputType.Text;

    [Parameter]
    public AutocompleteType Autocomplete { get; set; } = AutocompleteType.Off;

    [Parameter]
    public bool AutoFocus { get; set;} = false;

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<TValue> OnBlur { get; set; }

    [Parameter]
    public EventCallback<TValue> OnEnter { get; set; }

    [Parameter]
    public EventCallback<ElementReference> InputRefChanged { get; set; }

    [Parameter]
    public Func<TValue, (bool IsValid, string? ErrorMessage)>? Validator { get; set; }

    [Parameter]
    public string ErrorMessage { get; set; } = string.Empty;

    [Parameter]
    public string ErrorClass { get; set; } = "input-error";

    protected bool IsValid { get; set; } = true;
    protected string? ValidationMessage { get; set; }

    protected override Task OnParametersSetAsync()
    {
        currentValue = Value;
        return base.OnParametersSetAsync();
    } 

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (AutoFocus)
        {
            await inputElement.FocusAsync();
        }
    }

    protected override void BuildComponent(RenderTreeBuilder builder)
    {
        // 错误气泡框
        if (!IsValid && !string.IsNullOrEmpty(ValidationMessage))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-bubble");
            builder.AddContent(3, ValidationMessage);
            builder.CloseElement();
        }

        builder.OpenElement(10, "input");
        builder.AddAttribute(11, "class", ComputedClass);
        builder.AddAttribute(12, "style", ComputedStyle);
        builder.AddAttribute(13, "role", "input");

        BuildComponentAttributes(builder);

        // add element reference
        builder.AddElementReferenceCapture(21, async capturedRef =>
        {
            inputElement = capturedRef;

            if (InputRefChanged.HasDelegate)
            {
                await InputRefChanged.InvokeAsync(capturedRef);
            }
        });


        builder.CloseElement();
    }

    protected virtual void BuildComponentAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(14, "type", Type);
        builder.AddAttribute(15, "value", BindConverter.FormatValue(currentValue));

        builder.AddAttribute(16, "oninput", EventCallback.Factory.CreateBinder<TValue>(
                    this, __value => currentValue = __value, currentValue ?? default(TValue)!));

        builder.AddAttribute(17, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, async _ =>
        {
            await HandleBlur(currentValue);
        }));

        builder.AddAttribute(18, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, async args =>
        {
            await HandleEnter(currentValue, args);
        }));

        builder.AddAttribute(19, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, async _ =>
        {
            await ValueChanged.InvokeAsync(currentValue);
        }));

        builder.AddAttribute(20, "autocomplete", AutocompleteFactory.GetAutocomplete(Autocomplete));
    }

    private async Task HandleBlur(TValue? value)
    {
        if (OnBlur.HasDelegate)
        {
            await OnBlur.InvokeAsync(value ?? default(TValue)!);
        }
    }

    private async Task HandleEnter(TValue? value, KeyboardEventArgs args)
    {
        switch(args.Key)
        {
            case "Enter":
                if (OnEnter.HasDelegate)
                {
                    var res = await Validate();
                    if(res)
                    {
                        await OnEnter.InvokeAsync(value ?? default(TValue)!);
                    }
                }
                break;
            default:
#if DEBUG
                EventBus.Publish<InputKeyEvent> (new(this.Id.ToString(), args.Key));
#endif
                break;
        }
    }

    public async Task<bool> Validate()
    {
        if (Validator == null) return true;

        var (isValid, errorMessage) = Validator(currentValue);
        IsValid = isValid;
        ValidationMessage = errorMessage;

        // 触发重新渲染以显示验证状态
        StateHasChanged();

        return isValid;
    }

    public void ResetValidation()
    {
        IsValid = true;
        ValidationMessage = null;
        StateHasChanged();
    }
}
