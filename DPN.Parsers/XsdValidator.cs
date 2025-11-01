using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

public class XsdValidator
{
	private readonly XmlSchemaSet schemaSet;

	public XsdValidator(string xsdSchemaPath)
	{
		schemaSet = new XmlSchemaSet();
		schemaSet.Add(null, xsdSchemaPath);
		schemaSet.Compile();
	}

	public ValidationResult Validate(XDocument document)
	{
		var errors = new List<ValidationError>();
		document.Validate(schemaSet, (_, e) =>
		{
			errors.Add(new ValidationError
			{
				Severity = e.Severity,
				Message = e.Message,
				LineNumber = e.Exception?.LineNumber ?? 0,
				LinePosition = e.Exception?.LinePosition ?? 0
			});
		});

		return new ValidationResult
		{
			IsValid = errors.All(e => e.Severity != XmlSeverityType.Error),
			Errors = errors
		};
	}

	public bool IsValid(XDocument document)
	{
		return Validate(document).IsValid;
	}
}

public class ValidationResult
{
	public bool IsValid { get; set; }
	public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
	public XmlSeverityType Severity { get; set; }
	public string Message { get; set; } = string.Empty;
	public int LineNumber { get; set; }
	public int LinePosition { get; set; }
}