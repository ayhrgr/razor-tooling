// <auto-generated/>
#pragma warning disable 1591
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Markup_InCodeBlocksWithTagHelper_DesignTime
    {
        private global::DivTagHelper __DivTagHelper;
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
global::System.Object __typeHelper = "*, TestAssembly";

#line default
#line hidden
        }
        ))();
        }
        #pragma warning restore 219
        #pragma warning disable 0414
        private static System.Object __o = null;
        #pragma warning restore 0414
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
  
    var people = new Person[]
    {
        new Person() { Name = "Taylor", Age = 95, },
    };

    void PrintName(Person person)
    {
        

#line default
#line hidden
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
        __o = person.Name;

#line default
#line hidden
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
                               
    }

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
   PrintName(people[0]); 

#line default
#line hidden
#line 15 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
   await AnnounceBirthday(people[0]); 

#line default
#line hidden
        }
        #pragma warning restore 1998
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
            
    Task AnnounceBirthday(Person person)
    {
        var formatted = $"Mr. {person.Name}";
        

#line default
#line hidden
#line 22 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
                           __o = formatted;

#line default
#line hidden
        __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
#line 23 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
              

        

#line default
#line hidden
#line 26 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
         for (var i = 0; i < person.Age / 10; i++)
        {
            

#line default
#line hidden
#line 28 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
            __o = i;

#line default
#line hidden
#line 28 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
                                         
        }

#line default
#line hidden
#line 30 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
             

        if (person.Age < 20)
        {
            return Task.CompletedTask;
        }

        

#line default
#line hidden
#line 37 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Markup_InCodeBlocksWithTagHelper.cshtml"
                               
        return Task.CompletedTask;
    }


    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

#line default
#line hidden
    }
}
#pragma warning restore 1591
