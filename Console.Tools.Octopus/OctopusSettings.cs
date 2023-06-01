// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq.Expressions;
using System.Reflection;

namespace Console.Tools.Octopus;

public class OctopusSettings : CommandSettings
{
    /// <summary>
    /// The Octopus Server Url.  Can be specified more than once.
    /// </summary>
    [CommandOption("-u|--server-url <server-url>")]
    public string[] ServerUrls { get; set; } = Array.Empty<string>();

    /// <summary>
    /// todo: if not, we can always use either a yaml config file or csv instead... 
    /// (would need to write some validation in these scenarios)
    /// </summary>
    [CommandOption("-t|--service-now-task-number <service-now-task-number>")]
    public string? ServiceNowTaskNumber { get; set; }



	public override ValidationResult Validate() 
	{
		if (!ServerUrls.Any())
		{
			return ValidationResult.Error($"VALIDATION FAILED:  Please provide at least one Octopus Server Url --{GetCommandOptionLongName<OctopusSettings>(x => x.ServerUrls)}");
		}

		return base.Validate();
	}


	protected static string GetCommandOptionLongName<TCommandSettings>(Expression<Func<TCommandSettings, object?>> propertyExpression)
	{
		PropertyInfo propertyInfo = GetPropertyInfo(propertyExpression);

		string longName = propertyInfo
			?.GetCustomAttribute<CommandOptionAttribute>()
			?.LongNames
			.First() ?? string.Empty;

		return longName;
	}


	protected static PropertyInfo GetPropertyInfo<TSource, TProperty>(
	Expression<Func<TSource, TProperty>> propertyExpression)
	{
		if (propertyExpression.Body is not MemberExpression member)
		{
			throw new ArgumentException(string.Format(
				"Expression '{0}' refers to a method, not a property.",
				propertyExpression.ToString()));
		}

		if (member.Member is not PropertyInfo propInfo)
		{
			throw new ArgumentException(string.Format(
				"Expression '{0}' refers to a field, not a property.",
				propertyExpression.ToString()));
		}

		Type type = typeof(TSource);
		if (propInfo.ReflectedType != null && type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
		{
			throw new ArgumentException(string.Format(
				"Expression '{0}' refers to a property that is not from type {1}.",
				propertyExpression.ToString(),
				type));
		}

		return propInfo;
	}
}