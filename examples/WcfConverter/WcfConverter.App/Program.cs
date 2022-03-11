using System.Reflection;
using ProtoBuf.Grpc.WcfConverter;

var convertor = new Convertor();
if (args.Length < 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine($"{IO.Path.GetFileName(Assembly.GetExecutingAssembly().Location)} ApplicationFolder OutputFolder [ServiceNames+]");
}

var (applicationFolder, outputFolder, serviceNames) = (args[0], args[1], args.Skip(2).ToArray());

var services = convertor.ConvertServices(applicationFolder, serviceNames);
foreach (var (service, protobuf) in services)
{
    string outfile = IO.Path.Combine(outputFolder, service + ".proto");
    IO.File.WriteAllText(outfile, protobuf);
}

