using System.Reflection;
using ProtoBuf.Grpc.WcfConverter;

var convertor = new Convertor();
if (args.Length < 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine($"{IO.Path.GetFileName(Assembly.GetExecutingAssembly().Location)} ApplicationFolder OutputFolder [ServiceNames+]");
}

//TODO: Write to args[1]
convertor.ConvertServices(args[0], args.Skip(2).ToArray());

