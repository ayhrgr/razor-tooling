// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
#nullable restore
#line 1 "x:\dir\subdir\Test\TestComponent.cshtml"
using Microsoft.AspNetCore.Components.Web;

#line default
#line hidden
#nullable disable
    public partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            __builder.OpenElement(0, "input");
            __builder.AddEventPreventDefaultAttribute(1, "onfocus", 
#nullable restore
#line 2 "x:\dir\subdir\Test\TestComponent.cshtml"
                                true

#line default
#line hidden
#nullable disable
            );
            __builder.AddEventStopPropagationAttribute(2, "onclick", 
#nullable restore
#line 2 "x:\dir\subdir\Test\TestComponent.cshtml"
                                                                Foo

#line default
#line hidden
#nullable disable
            );
            __builder.AddEventStopPropagationAttribute(3, "onfocus", 
#nullable restore
#line 2 "x:\dir\subdir\Test\TestComponent.cshtml"
                                                                                               false

#line default
#line hidden
#nullable disable
            );
            __builder.CloseElement();
        }
        #pragma warning restore 1998
#nullable restore
#line 3 "x:\dir\subdir\Test\TestComponent.cshtml"
       
    bool Foo { get; set; }

#line default
#line hidden
#nullable disable
    }
}
#pragma warning restore 1591
