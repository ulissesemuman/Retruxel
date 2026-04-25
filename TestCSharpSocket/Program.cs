using System.Net.Sockets;
using System.Text;

Console.WriteLine("Testando conexão C# -> Python...");

try
{
    using var client = new TcpClient();
    await client.ConnectAsync("127.0.0.1", 8888);
    Console.WriteLine("✓ Conectado!");
    
    var stream = client.GetStream();
    
    var message = "HELLO FROM C#\n";
    var data = Encoding.UTF8.GetBytes(message);
    await stream.WriteAsync(data);
    Console.WriteLine($"✓ Enviado: {message.Trim()}");
    
    var buffer = new byte[1024];
    var cts = new CancellationTokenSource(2000);
    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    Console.WriteLine($"✓ Recebido: {response.Trim()}");
    
    if (response.Contains("OK"))
    {
        Console.WriteLine("✓ Teste bem-sucedido!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Erro: {ex.Message}");
}

Console.WriteLine("\nPressione Enter para sair...");
Console.ReadLine();
