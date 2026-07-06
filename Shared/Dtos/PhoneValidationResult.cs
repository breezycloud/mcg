namespace Shared.Dtos;

// Wraps a nullable Guid so a "no match" result serializes as a real JSON body
// ({"matchedId":null}) instead of a bare null, which ASP.NET Core's
// HttpNoContentOutputFormatter turns into an empty 204 response that
// ReadFromJsonAsync can't parse.
public class PhoneValidationResult
{
    public Guid? MatchedId { get; set; }
}
