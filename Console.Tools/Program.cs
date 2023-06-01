// See https://aka.ms/new-console-template for more information
using System.Text;

// required by spectre.console for emoji and markup support... (must be first instruction in main!!!)
System.Console.OutputEncoding = Encoding.UTF8;

CommandAppFactory
	.Create()
	.Configure()
	.Run(args);
