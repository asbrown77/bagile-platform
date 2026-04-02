using Bagile.Application.Templates;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.UnitTests.Application.Templates;

/// <summary>
/// Unit tests for TemplateVariableSubstitution.Apply — Sprint 21 item 9.
/// </summary>
[TestFixture]
public class TemplateVariableSubstitutionTests
{
    // ── Happy-path ───────────────────────────────────────────────────────────

    [Test]
    public void Apply_Replaces_Single_Variable()
    {
        var result = TemplateVariableSubstitution.Apply(
            "Hello {{name}}",
            new Dictionary<string, string> { ["name"] = "Alex" });

        result.Should().Be("Hello Alex");
    }

    [Test]
    public void Apply_Replaces_All_Variables_In_One_Pass()
    {
        var template = "Course: {{course_name}}, Trainer: {{trainer_name}}, Dates: {{dates}}";
        var vars = new Dictionary<string, string>
        {
            ["course_name"]  = "Professional Scrum Master",
            ["trainer_name"] = "Chris Bexon",
            ["dates"]        = "27–28 April 2026",
        };

        var result = TemplateVariableSubstitution.Apply(template, vars);

        result.Should().Be("Course: Professional Scrum Master, Trainer: Chris Bexon, Dates: 27–28 April 2026");
    }

    [Test]
    public void Apply_Is_Case_Insensitive_On_Variable_Keys()
    {
        // Template uses lower-case, variable dictionary uses mixed case
        var result = TemplateVariableSubstitution.Apply(
            "Hello {{TRAINER_NAME}}",
            new Dictionary<string, string> { ["trainer_name"] = "Alex Brown" });

        result.Should().Be("Hello Alex Brown");
    }

    [Test]
    public void Apply_Preserves_HTML_Structure()
    {
        var template = "<p>Hi {{first_name}},</p><p>See you on <strong>{{dates}}</strong>.</p>";
        var vars = new Dictionary<string, string>
        {
            ["first_name"] = "Jo",
            ["dates"]      = "27 April 2026",
        };

        var result = TemplateVariableSubstitution.Apply(template, vars);

        result.Should().Be("<p>Hi Jo,</p><p>See you on <strong>27 April 2026</strong>.</p>");
    }

    [Test]
    public void Apply_Replaces_Variable_Inside_Href_Attribute()
    {
        var template = @"<a href=""{{zoom_url}}"">Join Zoom</a>";
        var vars = new Dictionary<string, string>
        {
            ["zoom_url"] = "https://zoom.us/j/12345",
        };

        var result = TemplateVariableSubstitution.Apply(template, vars);

        result.Should().Be(@"<a href=""https://zoom.us/j/12345"">Join Zoom</a>");
    }

    [Test]
    public void Apply_Replaces_Same_Variable_Multiple_Times()
    {
        var template = "{{name}} booked a course. Welcome, {{name}}!";
        var vars = new Dictionary<string, string> { ["name"] = "Alex" };

        var result = TemplateVariableSubstitution.Apply(template, vars);

        result.Should().Be("Alex booked a course. Welcome, Alex!");
    }

    // ── Missing variables ────────────────────────────────────────────────────

    [Test]
    public void Apply_Leaves_Unknown_Variables_As_Is()
    {
        var result = TemplateVariableSubstitution.Apply(
            "Hello {{unknown_var}}",
            new Dictionary<string, string> { ["name"] = "Alex" });

        // Placeholder must be preserved verbatim so the caller can detect missing data
        result.Should().Be("Hello {{unknown_var}}");
    }

    [Test]
    public void Apply_Partial_Variables_Replaces_Only_Known_Ones()
    {
        var template = "Trainer: {{trainer_name}}, Venue: {{venue_address}}";
        var vars = new Dictionary<string, string>
        {
            ["trainer_name"] = "Chris Bexon",
            // venue_address intentionally omitted
        };

        var result = TemplateVariableSubstitution.Apply(template, vars);

        result.Should().Be("Trainer: Chris Bexon, Venue: {{venue_address}}");
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Test]
    public void Apply_Empty_Template_Returns_Empty_String()
    {
        var result = TemplateVariableSubstitution.Apply(
            "",
            new Dictionary<string, string> { ["name"] = "Alex" });

        result.Should().BeEmpty();
    }

    [Test]
    public void Apply_No_Variables_Returns_Template_Unchanged()
    {
        const string template = "<p>No variables here.</p>";

        var result = TemplateVariableSubstitution.Apply(
            template,
            new Dictionary<string, string>());

        result.Should().Be(template);
    }

    [Test]
    public void Apply_Empty_Variable_Value_Replaces_With_Empty_String()
    {
        var result = TemplateVariableSubstitution.Apply(
            "Venue: {{venue_address}}",
            new Dictionary<string, string> { ["venue_address"] = "" });

        result.Should().Be("Venue: ");
    }

    [Test]
    public void Apply_Variable_Value_Containing_HTML_Entities_Is_Not_Double_Encoded()
    {
        // Values come from DB and may already contain HTML — we must not double-encode
        var result = TemplateVariableSubstitution.Apply(
            "<p>{{message}}</p>",
            new Dictionary<string, string> { ["message"] = "Day 1 AM: Introductions &amp; theory" });

        result.Should().Be("<p>Day 1 AM: Introductions &amp; theory</p>");
    }
}
