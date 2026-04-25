import socket
import time

host = '127.0.0.1'
port = 8888

print(f"Tentando conectar em {host}:{port}...")

try:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect((host, port))
        print("✓ Conectado!")
        
        # Envia mensagem
        message = "HELLO FROM PYTHON\n"
        s.sendall(message.encode('utf-8'))
        print(f"✓ Enviado: {message.strip()}")
        
        # Recebe resposta
        response = s.recv(1024).decode('utf-8')
        print(f"✓ Recebido: {response.strip()}")
        
        if "OK" in response:
            print("✓ Teste bem-sucedido!")
        else:
            print("✗ Resposta inesperada")
            
except ConnectionRefusedError:
    print("✗ Conexão recusada - Mesen não está rodando ou script não carregou")
except Exception as e:
    print(f"✗ Erro: {e}")

input("\nPressione Enter para sair...")
